using BusinessLogic.Dtos.User;
using BusinessLogic.Enums.Types;
using BusinessLogic.Json.Models;
using BusinessLogic.Services;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Voyager.API.Commands.Handlers;
using Voyager.API.Views;

namespace VoyagerTests.Commands;

/// <summary>
/// Tests for <see cref="ProfileHandler"/> — the testable core extracted from
/// the <c>/profile</c> command group (<c>view</c>, <c>create</c>, <c>change</c>,
/// and the <c>settings</c> page view). The user service is mocked; a real
/// <see cref="ViewFactory"/> (with a real <see cref="EnumService"/>) builds
/// the message so the rendered output is exercised too.
/// </summary>
[TestClass]
public class ProfileHandlerTests
{
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly IEnumService _enumService = new EnumService();
    private ProfileHandler _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        var viewFactory = new ViewFactory(false, _enumService, new ServerSettings());
        _sut = new ProfileHandler(_userService, _enumService, viewFactory);
    }

    // ---- ViewAsync ------------------------------------------------------

    [TestMethod]
    public async Task ViewAsync_WhenUserExistsAndCallerIsOther_ReturnsProfileWithoutDropdown()
    {
        var dto = new UserReadDto(77, "Grace", new UserSettings(Color.Blue));
        _userService.GetByIdAsync(77).Returns(dto);

        var message = await _sut.ViewAsync(userId: 77, callerId: 99);

        Assert.HasCount(1, message.Embeds);
        Assert.AreEqual("Grace's Profile", message.Embeds[0].Title);
        // Non-ephemeral other-user view only has the Clear button row.
        Assert.HasCount(1, message.Components);
        Assert.IsInstanceOfType<DiscordButtonComponent>(message.Components[0].Components.Single());
    }

    [TestMethod]
    public async Task ViewAsync_WhenUserExistsAndCallerIsOwner_IncludesPageDropdown()
    {
        var dto = new UserReadDto(77, "Grace", new UserSettings(Color.Blue));
        _userService.GetByIdAsync(77).Returns(dto);

        var message = await _sut.ViewAsync(userId: 77, callerId: 77);

        Assert.HasCount(1, message.Embeds);
        Assert.AreEqual("Grace's Profile", message.Embeds[0].Title);
        // Non-ephemeral own-profile view has both the page dropdown row and Clear button row.
        Assert.HasCount(2, message.Components);
        Assert.IsInstanceOfType<DiscordSelectComponent>(message.Components[0].Components.Single());
    }

    [TestMethod]
    public async Task ViewAsync_WhenUserMissing_ReturnsErrorNotification()
    {
        _userService.GetByIdAsync(Arg.Any<ulong>()).Returns((UserReadDto?)null);

        var message = await _sut.ViewAsync(userId: 999, callerId: 999);

        Assert.HasCount(1, message.Embeds);
        Assert.AreEqual("Error: User not found.", message.Embeds[0].Title);
    }

    [TestMethod]
    public async Task ViewAsync_QueriesTheGivenUserId()
    {
        _userService.GetByIdAsync(Arg.Any<ulong>()).Returns((UserReadDto?)null);

        await _sut.ViewAsync(userId: 555, callerId: 111);

        await _userService.Received(1).GetByIdAsync(555);
    }

    // ---- CreateAsync ----------------------------------------------------

    [TestMethod]
    public async Task CreateAsync_WhenUserAlreadyExists_ReturnsWarningAndDoesNotCreate()
    {
        _userService.GetByIdAsync(10).Returns(new UserReadDto(10, "Ada", new UserSettings()));

        var message = await _sut.CreateAsync(10, "Ada", "Red");

        Assert.AreEqual("Warning: You already have a profile.", message.Embeds[0].Title);
        await _userService.DidNotReceive().CreateAsync(Arg.Any<UserCreateDto>());
    }

    [TestMethod]
    public async Task CreateAsync_WhenNewUserWithoutColor_CreatesWithDefaultColor()
    {
        _userService.GetByIdAsync(10).Returns((UserReadDto?)null);

        var message = await _sut.CreateAsync(10, "Ada", null);

        Assert.AreEqual("Profile created successfully.", message.Embeds[0].Title);
        await _userService.Received(1).CreateAsync(Arg.Is<UserCreateDto>(dto =>
            dto.Id == 10 &&
            dto.Name == "Ada" &&
            dto.Settings.Color == Color.Teal &&
            dto.Settings.GamePrivacy == PrivacyOption.Public &&
            dto.Settings.CharacterPrivacy == PrivacyOption.Public));
    }

    [TestMethod]
    public async Task CreateAsync_WhenNewUserWithColor_CreatesWithMappedColor()
    {
        _userService.GetByIdAsync(10).Returns((UserReadDto?)null);

        var message = await _sut.CreateAsync(10, "Ada", "CrimsonUnknown");
        Assert.AreEqual("Profile created successfully.", message.Embeds[0].Title);

        var messageValid = await _sut.CreateAsync(11, "Grace", "HotPink");
        Assert.AreEqual("Profile created successfully.", messageValid.Embeds[0].Title);

        await _userService.Received(1).CreateAsync(Arg.Is<UserCreateDto>(dto =>
            dto.Id == 11 && dto.Settings.Color == Color.HotPink));
    }

    [TestMethod]
    public async Task CreateAsync_WhenServiceThrows_ReturnsErrorNotification()
    {
        _userService.GetByIdAsync(10).Returns((UserReadDto?)null);
        _userService.CreateAsync(Arg.Any<UserCreateDto>()).ThrowsAsync(new InvalidOperationException("DB down"));

        var message = await _sut.CreateAsync(10, "Ada", "Blue");

        Assert.AreEqual("An error occurred while creating the profile.", message.Embeds[0].Title);
    }

    // ---- ChangeAsync ----------------------------------------------------

    [TestMethod]
    public async Task ChangeAsync_WhenNoOptionsProvided_ReturnsWarningWithoutDatabaseCall()
    {
        var message = await _sut.ChangeAsync(10, "Ada", color: null, gamePrivacy: null, characterPrivacy: null);

        Assert.AreEqual("Warning: No settings were provided to change.", message.Embeds[0].Title);
        await _userService.DidNotReceive().GetByIdAsync(Arg.Any<ulong>());
        await _userService.DidNotReceive().UpdateAsync(Arg.Any<UserUpdateDto>());
    }

    [TestMethod]
    public async Task ChangeAsync_WhenUserMissing_ReturnsErrorAndDoesNotUpdate()
    {
        _userService.GetByIdAsync(10).Returns((UserReadDto?)null);

        var message = await _sut.ChangeAsync(10, "Ada", color: "Red");

        Assert.AreEqual("Error: User not found.", message.Embeds[0].Title);
        await _userService.DidNotReceive().UpdateAsync(Arg.Any<UserUpdateDto>());
    }

    [TestMethod]
    public async Task ChangeAsync_WhenPartialOptionsProvided_UpdatesOnlyProvidedSettings()
    {
        var existing = new UserReadDto(
            10,
            "Ada",
            new UserSettings(Color.Gold, PrivacyOption.Public, PrivacyOption.Private));
        _userService.GetByIdAsync(10).Returns(existing);

        var message = await _sut.ChangeAsync(
            10,
            "Ada",
            color: null,
            gamePrivacy: PrivacyOption.Private,
            characterPrivacy: null);

        Assert.AreEqual("Settings updated successfully.", message.Embeds[0].Title);
        await _userService.Received(1).UpdateAsync(Arg.Is<UserUpdateDto>(dto =>
            dto.Id == 10 &&
            dto.Name == "Ada" &&
            dto.Settings.Color == Color.Gold &&
            dto.Settings.GamePrivacy == PrivacyOption.Private &&
            dto.Settings.CharacterPrivacy == PrivacyOption.Private));
    }

    [TestMethod]
    public async Task ChangeAsync_WhenAllOptionsProvided_UpdatesAllSettings()
    {
        var existing = new UserReadDto(
            10,
            "Ada",
            new UserSettings(Color.Teal, PrivacyOption.Public, PrivacyOption.Public));
        _userService.GetByIdAsync(10).Returns(existing);

        var message = await _sut.ChangeAsync(
            10,
            "Ada",
            color: "Violet",
            gamePrivacy: PrivacyOption.Private,
            characterPrivacy: PrivacyOption.Private);

        Assert.AreEqual("Settings updated successfully.", message.Embeds[0].Title);
        await _userService.Received(1).UpdateAsync(Arg.Is<UserUpdateDto>(dto =>
            dto.Id == 10 &&
            dto.Settings.Color == Color.Violet &&
            dto.Settings.GamePrivacy == PrivacyOption.Private &&
            dto.Settings.CharacterPrivacy == PrivacyOption.Private));
    }

    [TestMethod]
    public async Task ChangeAsync_WhenServiceThrows_ReturnsErrorNotification()
    {
        var existing = new UserReadDto(10, "Ada", new UserSettings());
        _userService.GetByIdAsync(10).Returns(existing);
        _userService.UpdateAsync(Arg.Any<UserUpdateDto>()).ThrowsAsync(new InvalidOperationException("DB error"));

        var message = await _sut.ChangeAsync(10, "Ada", color: "Blue");

        Assert.AreEqual("An error occurred while updating the settings.", message.Embeds[0].Title);
    }

    // ---- SettingsAsync --------------------------------------------------

    [TestMethod]
    public async Task SettingsAsync_WhenUserExists_ReturnsSettingsEmbedWithDropdown()
    {
        var dto = new UserReadDto(77, "Grace", new UserSettings(Color.Blue));
        _userService.GetByIdAsync(77).Returns(dto);

        var message = await _sut.SettingsAsync(77);

        Assert.HasCount(1, message.Embeds);
        Assert.AreEqual("Grace's Settings", message.Embeds[0].Title);
        Assert.IsInstanceOfType<DiscordSelectComponent>(message.Components[0].Components.Single());
    }

    [TestMethod]
    public async Task SettingsAsync_WhenUserMissing_ReturnsErrorNotification()
    {
        _userService.GetByIdAsync(999).Returns((UserReadDto?)null);

        var message = await _sut.SettingsAsync(999);

        Assert.AreEqual("Error: User not found.", message.Embeds[0].Title);
    }
}
