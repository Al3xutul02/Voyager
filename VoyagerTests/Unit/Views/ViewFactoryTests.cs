using BusinessLogic.Dtos.DiceRoll;
using BusinessLogic.Dtos.Server;
using BusinessLogic.Dtos.User;
using BusinessLogic.Enums.Types;
using BusinessLogic.Json.Models;
using BusinessLogic.Services;
using DSharpPlus.Entities;
using Voyager.API.Enums.Types;
using Voyager.API.Views;

namespace VoyagerTests.Views;

/// <summary>
/// Tests for <see cref="ViewFactory"/> covering ephemeral vs. non-ephemeral
/// component attachment, privacy masking, the profile/settings page
/// switcher dropdown, and server profile embeds.
/// </summary>
[TestClass]
public class ViewFactoryTests
{
    private readonly ServerSettings _settings = new();
    private ViewFactory _nonEphemeralSut = null!;
    private ViewFactory _ephemeralSut = null!;

    [TestInitialize]
    public void Setup()
    {
        var enumService = new EnumService();
        _nonEphemeralSut = new ViewFactory(false, enumService, _settings);
        _ephemeralSut = new ViewFactory(true, enumService, _settings);
    }

    // ---- CreateNotification ---------------------------------------------

    [TestMethod]
    public void CreateNotification_Error_NonEphemeral_BuildsEmbedWithErrorColorAndClearButton()
    {
        var message = _nonEphemeralSut.CreateNotification(NotificationType.Error, "Something broke");

        Assert.HasCount(1, message.Embeds);
        DiscordEmbed embed = message.Embeds[0];
        Assert.AreEqual("Something broke", embed.Title);
        Assert.AreEqual(new EnumService().ConvertColor(_settings.ErrorColor).Value, embed.Color.Value.Value);

        // A clear button row is attached when non-ephemeral.
        Assert.HasCount(1, message.Components);
    }

    [TestMethod]
    public void CreateNotification_Ephemeral_OmitsClearButton()
    {
        var message = _ephemeralSut.CreateNotification(NotificationType.General, "Info");

        Assert.HasCount(1, message.Embeds);
        Assert.IsEmpty(message.Components);
    }

    [TestMethod]
    public void CreateNotification_Success_UsesSuccessColor()
    {
        var message = _nonEphemeralSut.CreateNotification(NotificationType.Success, "Done");

        Assert.AreEqual(
            new EnumService().ConvertColor(_settings.SuccessColor).Value,
            message.Embeds[0].Color.Value.Value);
    }

    [TestMethod]
    public void CreateNotification_Warning_UsesWarningColor()
    {
        var message = _nonEphemeralSut.CreateNotification(NotificationType.Warning, "Careful");

        Assert.AreEqual(
            new EnumService().ConvertColor(_settings.WarningColor).Value,
            message.Embeds[0].Color.Value.Value);
    }

    // ---- CreateUserProfile ----------------------------------------------

    [TestMethod]
    public void CreateUserProfile_OtherUser_BuildsProfileEmbedWithoutPageSelect()
    {
        var dto = new UserReadDto(123, "Ada", new UserSettings(Color.Red));

        var message = _nonEphemeralSut.CreateUserProfile(dto, callerId: 999);

        Assert.HasCount(1, message.Embeds);
        DiscordEmbed embed = message.Embeds[0];
        Assert.AreEqual("Ada's Profile", embed.Title);
        Assert.AreEqual(DiscordColor.Red.Value, embed.Color.Value.Value);

        // Only the Clear button row is present (no page-switcher dropdown).
        Assert.HasCount(1, message.Components);
        Assert.IsInstanceOfType<DiscordButtonComponent>(message.Components[0].Components.Single());
    }

    [TestMethod]
    public void CreateUserProfile_OtherUserWithPrivateSettings_HidesGamesAndCharacters()
    {
        var dto = new UserReadDto(
            123,
            "Ada",
            new UserSettings(Color.Red, PrivacyOption.Private, PrivacyOption.Private));

        var message = _ephemeralSut.CreateUserProfile(dto, callerId: 999);

        DiscordEmbed embed = message.Embeds[0];
        Assert.AreEqual("_Games hidden by user_", embed.Fields[0].Value);
        Assert.AreEqual("_Characters hidden by user_", embed.Fields[1].Value);
        Assert.IsEmpty(message.Components);
    }

    [TestMethod]
    public void CreateUserProfile_OwnProfile_IncludesPageSelectAndShowsPrivateFields()
    {
        var dto = new UserReadDto(
            123,
            "Ada",
            new UserSettings(Color.Red, PrivacyOption.Private, PrivacyOption.Private));

        var message = _nonEphemeralSut.CreateUserProfile(dto, callerId: 123);

        DiscordEmbed embed = message.Embeds[0];
        Assert.AreEqual("_No Current Games_", embed.Fields[0].Value);
        Assert.AreEqual("_No Current Characters_", embed.Fields[1].Value);

        // Row 0: page-switcher dropdown; Row 1: clear button.
        Assert.HasCount(2, message.Components);
        var select = (DiscordSelectComponent)message.Components[0].Components.Single();
        Assert.AreEqual("switchProfilePage", select.CustomId);
        Assert.IsTrue(select.Options.Single(o => o.Value == nameof(ProfilePage.Profile)).Default);
        Assert.IsFalse(select.Options.Single(o => o.Value == nameof(ProfilePage.Settings)).Default);
    }

    // ---- CreateUserSettings ---------------------------------------------

    [TestMethod]
    public void CreateUserSettings_BuildsSettingsEmbedWithPageSelectDefaultedToSettings()
    {
        var dto = new UserReadDto(
            123,
            "Ada",
            new UserSettings(Color.Purple, PrivacyOption.Private, PrivacyOption.Public));

        var message = _ephemeralSut.CreateUserSettings(dto);

        Assert.HasCount(1, message.Embeds);
        DiscordEmbed embed = message.Embeds[0];
        Assert.AreEqual("Ada's Settings", embed.Title);
        Assert.AreEqual(DiscordColor.Purple.Value, embed.Color.Value.Value);
        StringAssert.Contains(embed.Fields[0].Value, "Purple");
        StringAssert.Contains(embed.Fields[1].Value, "Private");
        StringAssert.Contains(embed.Fields[1].Value, "Public");

        // Ephemeral: only the page-switcher dropdown row is attached.
        Assert.HasCount(1, message.Components);
        var select = (DiscordSelectComponent)message.Components[0].Components.Single();
        Assert.AreEqual("switchProfilePage", select.CustomId);
        Assert.IsTrue(select.Options.Single(o => o.Value == nameof(ProfilePage.Settings)).Default);
        Assert.IsFalse(select.Options.Single(o => o.Value == nameof(ProfilePage.Profile)).Default);
    }

    // ---- CreateServerProfile --------------------------------------------

    [TestMethod]
    public void CreateServerProfile_WithDescription_BuildsEmbedWithNameColorDescriptionAndModeFooter()
    {
        var dto = new ServerReadDto(
            100,
            "Golarion Guild",
            "Welcome to our Westmarch server!",
            200,
            300,
            new ServerSettings(ServerMode.Westmarch, Color.Gold));

        var message = _nonEphemeralSut.CreateServerProfile(dto);

        Assert.HasCount(1, message.Embeds);
        DiscordEmbed embed = message.Embeds[0];
        Assert.AreEqual("Golarion Guild", embed.Title);
        Assert.AreEqual("Welcome to our Westmarch server!", embed.Description);
        Assert.AreEqual(DiscordColor.Gold.Value, embed.Color.Value.Value);
        Assert.AreEqual("Server Mode: Westmarch", embed.Footer.Text);
        Assert.HasCount(1, message.Components);
    }

    [TestMethod]
    public void CreateServerProfile_WithoutDescription_UsesPlaceholderAndOmitsClearButtonWhenEphemeral()
    {
        var dto = new ServerReadDto(
            100,
            "Quiet Server",
            null,
            200,
            300,
            new ServerSettings(ServerMode.Normal, Color.Blurple));

        var message = _ephemeralSut.CreateServerProfile(dto);

        Assert.HasCount(1, message.Embeds);
        DiscordEmbed embed = message.Embeds[0];
        Assert.AreEqual("Quiet Server", embed.Title);
        Assert.AreEqual("_No description provided._", embed.Description);
        Assert.AreEqual(DiscordColor.Blurple.Value, embed.Color.Value.Value);
        Assert.AreEqual("Server Mode: Normal", embed.Footer.Text);
        Assert.IsEmpty(message.Components);
    }

    // ---- CreateDiceRoll & CreateDiceGuide -------------------------------

    [TestMethod]
    public void CreateDiceRoll_AttachesDiceRollEmbedAndSaveAndClearButtons()
    {
        var term = new DiceTermResultDto("2d6", true, 1, [new RolledDieDto(4, true), new RolledDieDto(6, true)], 10, 2, 12, 7.0, 5.833);
        var section = new DiceSectionResultDto("(2d6)[fire]", "fire", [term], 10, 2, 12, 7.0, 5.833, 2.415);
        var tagSummary = new DiceTagSummaryDto("fire", 10, 2, 12, 7.0, 2.415);
        var eval = new DiceEvaluationResultDto("(2d6)[fire]", null, [section], [tagSummary], 10, 2, 12, 7.0, 2.415);

        var message = _nonEphemeralSut.CreateDiceRoll(55, "Seoni", eval, Color.Red, "Scorching Ray");

        Assert.HasCount(1, message.Embeds);
        Assert.AreEqual("Seoni's Roll — Scorching Ray", message.Embeds[0].Title);
        Assert.AreEqual(DiscordColor.Red.Value, message.Embeds[0].Color.Value.Value);
        Assert.AreEqual("Roller ID: 55", message.Embeds[0].Footer.Text);

        Assert.HasCount(1, message.Components);
        var buttons = message.Components[0].Components.Cast<DiscordButtonComponent>().ToList();
        Assert.HasCount(2, buttons);
        Assert.AreEqual("saveRoll", buttons[0].CustomId);
        Assert.AreEqual("clearRoll", buttons[1].CustomId);
    }

    [TestMethod]
    public void CreateDiceGuide_EphemeralAndNonEphemeral_RendersGuideAndRespectsClearButtonRule()
    {
        var ephemeralGuide = _ephemeralSut.CreateDiceGuide();
        Assert.HasCount(1, ephemeralGuide.Embeds);
        StringAssert.Contains(ephemeralGuide.Embeds[0].Title, "Dice Roll Guide");
        Assert.IsEmpty(ephemeralGuide.Components);

        var nonEphemeralGuide = _nonEphemeralSut.CreateDiceGuide();
        Assert.HasCount(1, nonEphemeralGuide.Embeds);
        Assert.HasCount(1, nonEphemeralGuide.Components);
    }
}
