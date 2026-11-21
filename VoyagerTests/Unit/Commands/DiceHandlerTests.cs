using AutoMapper;
using BusinessLogic.Dtos.DiceRoll;
using BusinessLogic.Dtos.User;
using BusinessLogic.Enums.Types;
using BusinessLogic.Json.Models;
using BusinessLogic.Services;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;
using NSubstitute;
using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories.Abstractions;
using Voyager.API.Commands.Handlers;
using Voyager.API.Events;
using Voyager.API.Views;
using Voyager.API.Views.Components.Embeds;
using VoyagerTests.TestHelpers;

namespace VoyagerTests.Commands;

/// <summary>
/// Tests for <see cref="DiceHandler"/> (<c>/dice roll</c>, <c>/dice create</c>,
/// <c>/dice delete</c>, <c>/dice guide</c>, and the Save button pre-validation)
/// plus the restricted Clear button authorization logic in
/// <see cref="Voyager.API.Events.EventHandler.CanClearRoll"/>.
/// </summary>
[TestClass]
public class DiceHandlerTests
{
    private readonly IDiceRollRepository _diceRollRepository = Substitute.For<IDiceRollRepository>();
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly IEnumService _enumService = new EnumService();
    private readonly IMapper _mapper = MapperHelper.CreateMapper();
    private IDiceRollService _diceRollService = null!;
    private DiceHandler _sut = null!;

    private sealed class FixedRandom(int value) : Random
    {
        public override int Next(int minValue, int maxValue) => Math.Clamp(value, minValue, maxValue - 1);
    }

    [TestInitialize]
    public void Setup()
    {
        _diceRollService = new DiceRollService(_mapper, _diceRollRepository);
        var viewFactory = new ViewFactory(false, _enumService, new ServerSettings());
        _sut = new DiceHandler(_diceRollService, _userService, viewFactory);
    }

    // ---- RollAsync ------------------------------------------------------

    [TestMethod]
    public async Task RollAsync_CustomFormulaWithoutUserProfile_RollsAndIncludesSaveAndClearButtons()
    {
        _userService.GetByIdAsync(Arg.Any<ulong>()).Returns((UserReadDto?)null);

        var message = await _sut.RollAsync(
            callerId: 42,
            callerName: "Valeros",
            diceInput: "(2d6)[fire]+4[poison]",
            statistics: null,
            random: new FixedRandom(5));

        Assert.HasCount(1, message.Embeds);
        var embed = message.Embeds[0];
        Assert.AreEqual("Valeros's Dice Roll", embed.Title);
        StringAssert.Contains(embed.Fields.Single(f => f.Name == "**Result**").Value, "Total: 14 (10 fire + 4 poison)");

        // Verify Save and Clear buttons are attached
        Assert.HasCount(1, message.Components);
        var buttons = message.Components[0].Components.Cast<DiscordButtonComponent>().ToList();
        Assert.HasCount(2, buttons);
        Assert.AreEqual("saveRoll", buttons[0].CustomId);
        Assert.AreEqual("clearRoll", buttons[1].CustomId);

        // Verify formula and roller ID can be extracted from the embed for the button handlers
        Assert.AreEqual("(2d6)[fire]+4[poison]", Embeds.ExtractFormulaFromDiceRollEmbed(embed));
        Assert.IsTrue(Embeds.TryExtractRollerIdFromDiceRollEmbed(embed, out ulong extractedRollerId));
        Assert.AreEqual(42UL, extractedRollerId);
    }

    [TestMethod]
    public async Task RollAsync_WhenInputMatchesSavedDiceName_RollsSavedFormulaWithUserColor()
    {
        _diceRollRepository
            .GetByUserAndNameAsync(77, "Fireball", Arg.Any<IncludeBehavior>(), Arg.Any<Func<IQueryable<DiceRoll>, IQueryable<DiceRoll>>?>())
            .Returns(new DiceRoll { Id = 1, UserId = 77, Name = "Fireball", Formula = "(6d6)[fire]" });

        _userService.GetByIdAsync(77).Returns(new UserReadDto(77, "Ezren", new UserSettings(Color.Orange)));

        var message = await _sut.RollAsync(
            callerId: 77,
            callerName: "Ezren",
            diceInput: "Fireball",
            statistics: null,
            random: new FixedRandom(6));

        var embed = message.Embeds[0];
        Assert.AreEqual("Ezren's Roll — Fireball", embed.Title);
        Assert.AreEqual(DiscordColor.Orange.Value, embed.Color.Value.Value);
        StringAssert.Contains(embed.Fields.Single(f => f.Name == "**Result**").Value, "Total: 36 fire");
    }

    [TestMethod]
    public async Task RollAsync_WithStatisticsOption_DisplaysRequestedStatistics()
    {
        var message = await _sut.RollAsync(
            callerId: 77,
            callerName: "Ezren",
            diceInput: "2d6+4",
            statistics: DiceStatisticsType.All);

        var embed = message.Embeds[0];
        var statsField = embed.Fields.Single(f => f.Name == "**Statistics**").Value;
        StringAssert.Contains(statsField, "**Minimum:** 6");
        StringAssert.Contains(statsField, "**Maximum:** 16");
        StringAssert.Contains(statsField, "**Average:** 11");
        StringAssert.Contains(statsField, "**Standard Deviation:** 2.42");
    }

    [TestMethod]
    public async Task RollAsync_WithInvalidFormula_ReturnsErrorNotification()
    {
        var message = await _sut.RollAsync(77, "Ezren", "not_a_formula");

        Assert.AreEqual(DiscordColor.DarkRed.Value, message.Embeds[0].Color.Value.Value);
        StringAssert.StartsWith(message.Embeds[0].Title, "Error:");
    }

    // ---- CreateAsync & ValidateCanSaveAsync -----------------------------

    [TestMethod]
    public async Task CreateAsync_WhenUserHasNoProfile_ReturnsErrorRequiringProfile()
    {
        _userService.GetByIdAsync(10).Returns((UserReadDto?)null);

        var message = await _sut.CreateAsync(10, "Longsword", "1d8+4");

        StringAssert.Contains(message.Embeds[0].Title, "/profile create");
        await _diceRollRepository.DidNotReceive().AddAsync(Arg.Any<DiceRoll>());
    }

    [TestMethod]
    public async Task CreateAsync_WhenFormulaInvalid_ReturnsValidationError()
    {
        _userService.GetByIdAsync(10).Returns(new UserReadDto(10, "Kyra", new UserSettings()));

        var message = await _sut.CreateAsync(10, "BadRoll", "4d6h9");

        StringAssert.Contains(message.Embeds[0].Title, "Keep count");
        await _diceRollRepository.DidNotReceive().AddAsync(Arg.Any<DiceRoll>());
    }

    [TestMethod]
    public async Task CreateAsync_WhenFormulaAlreadySaved_ReturnsWarning()
    {
        _userService.GetByIdAsync(10).Returns(new UserReadDto(10, "Kyra", new UserSettings()));
        _diceRollRepository
            .GetByUserAndFormulaAsync(10, "4d6h2+4", Arg.Any<IncludeBehavior>(), Arg.Any<Func<IQueryable<DiceRoll>, IQueryable<DiceRoll>>?>())
            .Returns(new DiceRoll { Id = 2, UserId = 10, Name = "ExistingRoll", Formula = "4d6h2+4" });

        var message = await _sut.CreateAsync(10, "NewName", "4d6h2 + 4");

        StringAssert.Contains(message.Embeds[0].Title, "already saved as 'ExistingRoll'");
        await _diceRollRepository.DidNotReceive().AddAsync(Arg.Any<DiceRoll>());
    }

    [TestMethod]
    public async Task CreateAsync_WhenCreateLimitReached_ReturnsLimitError()
    {
        _userService.GetByIdAsync(10).Returns(new UserReadDto(10, "Kyra", new UserSettings()));
        var twentyFiveRolls = Enumerable.Range(1, IDiceRollService.MaxSavedDicePerUser)
            .Select(i => new DiceRoll { Id = (ulong)i, UserId = 10, Name = $"Roll{i}", Formula = $"1d{i + 1}" })
            .ToList();
        _diceRollRepository
            .GetByUserIdAsync(10, Arg.Any<IncludeBehavior>(), Arg.Any<Func<IQueryable<DiceRoll>, IQueryable<DiceRoll>>?>())
            .Returns(twentyFiveRolls);

        var message = await _sut.CreateAsync(10, "Roll26", "2d20h1");

        StringAssert.Contains(message.Embeds[0].Title, $"maximum limit of {IDiceRollService.MaxSavedDicePerUser}");
        await _diceRollRepository.DidNotReceive().AddAsync(Arg.Any<DiceRoll>());
    }

    [TestMethod]
    public async Task CreateAsync_WhenNameAlreadyExists_ReturnsWarning()
    {
        _userService.GetByIdAsync(10).Returns(new UserReadDto(10, "Kyra", new UserSettings()));
        _diceRollRepository
            .GetByUserIdAsync(10, Arg.Any<IncludeBehavior>(), Arg.Any<Func<IQueryable<DiceRoll>, IQueryable<DiceRoll>>?>())
            .Returns(new List<DiceRoll>());
        _diceRollRepository
            .GetByUserAndNameAsync(10, "Heal", Arg.Any<IncludeBehavior>(), Arg.Any<Func<IQueryable<DiceRoll>, IQueryable<DiceRoll>>?>())
            .Returns(new DiceRoll { Id = 3, UserId = 10, Name = "Heal", Formula = "1d8+8" });

        var message = await _sut.CreateAsync(10, "Heal", "2d8+16");

        StringAssert.Contains(message.Embeds[0].Title, "already have a saved dice configuration named 'Heal'");
        await _diceRollRepository.DidNotReceive().AddAsync(Arg.Any<DiceRoll>());
    }

    [TestMethod]
    public async Task CreateAsync_WhenValid_SavesNormalizedFormulaAndReturnsSuccess()
    {
        _userService.GetByIdAsync(10).Returns(new UserReadDto(10, "Kyra", new UserSettings()));
        _diceRollRepository
            .GetByUserIdAsync(10, Arg.Any<IncludeBehavior>(), Arg.Any<Func<IQueryable<DiceRoll>, IQueryable<DiceRoll>>?>())
            .Returns(new List<DiceRoll>());

        var message = await _sut.CreateAsync(10, "Fireball", " (6d6)[fire] ");

        StringAssert.Contains(message.Embeds[0].Title, "Saved dice configuration 'Fireball'");
        await _diceRollRepository.Received(1).AddAsync(Arg.Is<DiceRoll>(d =>
            d.UserId == 10 && d.Name == "Fireball" && d.Formula == "(6d6)[fire]"));
        await _diceRollRepository.Received(1).SaveAsync();
    }

    // ---- DeleteAsync ----------------------------------------------------

    [TestMethod]
    public async Task DeleteAsync_WhenSavedRollExists_DeletesAndReturnsSuccess()
    {
        _userService.GetByIdAsync(10).Returns(new UserReadDto(10, "Kyra", new UserSettings()));
        var stored = new DiceRoll { Id = 12, UserId = 10, Name = "Fireball", Formula = "(6d6)[fire]" };
        _diceRollRepository
            .GetByUserAndNameAsync(10, "Fireball", Arg.Any<IncludeBehavior>(), Arg.Any<Func<IQueryable<DiceRoll>, IQueryable<DiceRoll>>?>())
            .Returns(stored);
        _diceRollRepository
            .GetByIdAsync(12, Arg.Any<IncludeBehavior>(), Arg.Any<Func<IQueryable<DiceRoll>, IQueryable<DiceRoll>>?>())
            .Returns(stored);

        var message = await _sut.DeleteAsync(10, "Fireball");

        StringAssert.Contains(message.Embeds[0].Title, "Deleted saved dice configuration 'Fireball'");
        _diceRollRepository.Received(1).Delete(stored);
        await _diceRollRepository.Received(1).SaveAsync();
    }

    [TestMethod]
    public async Task DeleteAsync_WhenSavedRollNotFound_ReturnsError()
    {
        _userService.GetByIdAsync(10).Returns(new UserReadDto(10, "Kyra", new UserSettings()));

        var message = await _sut.DeleteAsync(10, "MissingRoll");

        StringAssert.Contains(message.Embeds[0].Title, "was not found");
        _diceRollRepository.DidNotReceive().Delete(Arg.Any<DiceRoll>());
    }

    // ---- Guide ----------------------------------------------------------

    [TestMethod]
    public void Guide_ReturnsGuideEmbedWithSyntaxAndExamples()
    {
        var message = _sut.Guide();

        Assert.HasCount(1, message.Embeds);
        var embed = message.Embeds[0];
        StringAssert.Contains(embed.Title, "Dice Roll Guide");
        Assert.HasCount(5, embed.Fields);
    }

    // ---- Restricted Clear Button Authorization --------------------------

    [TestMethod]
    public void CanClearRoll_AllowsRollerOrGmOrAdminOrServerOwner_AndDeniesOthers()
    {
        // Roller themselves -> allowed
        Assert.IsTrue(Voyager.API.Events.EventHandler.CanClearRoll(
            callerId: 100, rollerId: 100, isServerOwner: false, isAdmin: false, gmRoleId: 500, callerRoleIds: []));

        // Server owner -> allowed
        Assert.IsTrue(Voyager.API.Events.EventHandler.CanClearRoll(
            callerId: 200, rollerId: 100, isServerOwner: true, isAdmin: false, gmRoleId: 500, callerRoleIds: []));

        // Server admin -> allowed
        Assert.IsTrue(Voyager.API.Events.EventHandler.CanClearRoll(
            callerId: 200, rollerId: 100, isServerOwner: false, isAdmin: true, gmRoleId: 500, callerRoleIds: []));

        // GM (holds GMRoleId) -> allowed
        Assert.IsTrue(Voyager.API.Events.EventHandler.CanClearRoll(
            callerId: 200, rollerId: 100, isServerOwner: false, isAdmin: false, gmRoleId: 500, callerRoleIds: [400, 500]));

        // Other regular user -> denied
        Assert.IsFalse(Voyager.API.Events.EventHandler.CanClearRoll(
            callerId: 200, rollerId: 100, isServerOwner: false, isAdmin: false, gmRoleId: 500, callerRoleIds: [400]));
    }
}
