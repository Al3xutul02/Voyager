using BusinessLogic.Dtos.Server;
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
/// Tests for <see cref="ServerHandler"/> — the testable core extracted from
/// the <c>/server</c> command group (<c>setup</c>, <c>view</c>, <c>change</c>).
/// The server service is mocked; a real <see cref="ViewFactory"/> (with a real
/// <see cref="EnumService"/>) builds the message so the rendered output is
/// exercised too.
/// </summary>
[TestClass]
public class ServerHandlerTests
{
    private readonly IServerService _serverService = Substitute.For<IServerService>();
    private readonly IEnumService _enumService = new EnumService();
    private ServerHandler _sut = null!;

    /// <summary>
    /// Instantiates a DSharpPlus <see cref="SnowflakeObject"/> subclass
    /// (<see cref="DiscordGuild"/>, <see cref="DiscordRole"/>, <see cref="DiscordChannel"/>)
    /// via its internal constructor and assigns its <see cref="SnowflakeObject.Id"/>
    /// so unit tests can pass real entity objects without a live Discord connection.
    /// </summary>
    private static T CreateDiscordEntity<T>(ulong id) where T : SnowflakeObject
    {
        var entity = (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;
        typeof(SnowflakeObject).GetProperty(nameof(SnowflakeObject.Id))!
            .SetValue(entity, id);
        return entity;
    }

    [TestInitialize]
    public void Setup()
    {
        var viewFactory = new ViewFactory(true, _enumService, new ServerSettings());
        _sut = new ServerHandler(_serverService, _enumService, viewFactory);
    }

    // ---- SetupAsync -----------------------------------------------------

    [TestMethod]
    public async Task SetupAsync_WhenCallerIsNotServerOwner_ReturnsErrorAndDoesNotQueryDatabase()
    {
        var guild = CreateDiscordEntity<DiscordGuild>(100);

        var message = await _sut.SetupAsync(
            guild,
            isServerOwner: false,
            ServerMode.Normal,
            createGmRole: false,
            createAdminChannel: false,
            gmRole: CreateDiscordEntity<DiscordRole>(10),
            adminChannel: CreateDiscordEntity<DiscordChannel>(20));

        Assert.HasCount(1, message.Embeds);
        Assert.AreEqual("Error: Only the server owner can set up Voyager.", message.Embeds[0].Title);
        await _serverService.DidNotReceive().GetByIdAsync(Arg.Any<ulong>());
    }

    [TestMethod]
    public async Task SetupAsync_WhenCreateGmRoleFalseAndGmRoleNull_ReturnsError()
    {
        var guild = CreateDiscordEntity<DiscordGuild>(100);

        var message = await _sut.SetupAsync(
            guild,
            isServerOwner: true,
            ServerMode.Normal,
            createGmRole: false,
            createAdminChannel: false,
            gmRole: null,
            adminChannel: CreateDiscordEntity<DiscordChannel>(20));

        Assert.AreEqual("Error: GM role creation not requested and no existing role provided.", message.Embeds[0].Title);
        await _serverService.DidNotReceive().GetByIdAsync(Arg.Any<ulong>());
    }

    [TestMethod]
    public async Task SetupAsync_WhenCreateAdminChannelFalseAndAdminChannelNull_ReturnsError()
    {
        var guild = CreateDiscordEntity<DiscordGuild>(100);

        var message = await _sut.SetupAsync(
            guild,
            isServerOwner: true,
            ServerMode.Normal,
            createGmRole: false,
            createAdminChannel: false,
            gmRole: CreateDiscordEntity<DiscordRole>(10),
            adminChannel: null);

        Assert.AreEqual("Error: Admin channel creation not requested and no existing channel provided.", message.Embeds[0].Title);
        await _serverService.DidNotReceive().GetByIdAsync(Arg.Any<ulong>());
    }

    [TestMethod]
    public async Task SetupAsync_WhenServerAlreadyExists_ReturnsWarningAndDoesNotCreate()
    {
        var guild = CreateDiscordEntity<DiscordGuild>(100);
        var existing = new ServerReadDto(100, "Existing", null, 10, 20, new ServerSettings());
        _serverService.GetByIdAsync(100).Returns(existing);

        var message = await _sut.SetupAsync(
            guild,
            isServerOwner: true,
            ServerMode.Normal,
            createGmRole: false,
            createAdminChannel: false,
            gmRole: CreateDiscordEntity<DiscordRole>(10),
            adminChannel: CreateDiscordEntity<DiscordChannel>(20));

        Assert.AreEqual("Error: This server is already set up.", message.Embeds[0].Title);
        await _serverService.DidNotReceive().CreateAsync(Arg.Any<ServerCreateDto>());
    }

    [TestMethod]
    public async Task SetupAsync_WhenDiscordApiThrows_ReturnsErrorNotification()
    {
        var guild = CreateDiscordEntity<DiscordGuild>(100);
        _serverService.GetByIdAsync(100).Returns((ServerReadDto?)null);

        // An unattached DiscordGuild/DiscordChannel has no live DiscordClient ApiClient,
        // so resolving the bot member or adding overwrites throws inside the try block
        // and is caught cleanly.
        var message = await _sut.SetupAsync(
            guild,
            isServerOwner: true,
            ServerMode.Westmarch,
            createGmRole: false,
            createAdminChannel: false,
            gmRole: CreateDiscordEntity<DiscordRole>(10),
            adminChannel: CreateDiscordEntity<DiscordChannel>(20));

        Assert.AreEqual("Error: Something went wrong while setting up Voyager.", message.Embeds[0].Title);
    }

    // ---- ViewAsync ------------------------------------------------------

    [TestMethod]
    public async Task ViewAsync_WhenServerExists_ReturnsServerProfileEmbed()
    {
        var dto = new ServerReadDto(
            100,
            "Golarion",
            "Pathfinder 2E PbP",
            10,
            20,
            new ServerSettings(ServerMode.Westmarch, Color.Gold));
        _serverService.GetByIdAsync(100).Returns(dto);

        var message = await _sut.ViewAsync(100);

        Assert.HasCount(1, message.Embeds);
        Assert.AreEqual("Golarion", message.Embeds[0].Title);
        Assert.AreEqual("Pathfinder 2E PbP", message.Embeds[0].Description);
        Assert.AreEqual("Server Mode: Westmarch", message.Embeds[0].Footer.Text);
    }

    [TestMethod]
    public async Task ViewAsync_WhenServerMissing_ReturnsErrorNotification()
    {
        _serverService.GetByIdAsync(999).Returns((ServerReadDto?)null);

        var message = await _sut.ViewAsync(999);

        Assert.HasCount(1, message.Embeds);
        Assert.AreEqual("Error: This server is not set up yet.", message.Embeds[0].Title);
    }

    // ---- ChangeAsync ----------------------------------------------------

    [TestMethod]
    public async Task ChangeAsync_WhenNoOptionsProvided_ReturnsWarningWithoutDatabaseCall()
    {
        var message = await _sut.ChangeAsync(
            guildId: 100,
            gmRole: null,
            adminChannel: null,
            description: null,
            generalColor: null,
            errorColor: null,
            successColor: null,
            warningColor: null);

        Assert.AreEqual("Warning: No changes were provided.", message.Embeds[0].Title);
        await _serverService.DidNotReceive().GetByIdAsync(Arg.Any<ulong>());
        await _serverService.DidNotReceive().UpdateAsync(Arg.Any<ServerUpdateDto>());
    }

    [TestMethod]
    public async Task ChangeAsync_WhenServerMissing_ReturnsErrorAndDoesNotUpdate()
    {
        _serverService.GetByIdAsync(100).Returns((ServerReadDto?)null);

        var message = await _sut.ChangeAsync(
            guildId: 100,
            gmRole: null,
            adminChannel: null,
            description: "New description",
            generalColor: null,
            errorColor: null,
            successColor: null,
            warningColor: null);

        Assert.AreEqual("Error: This server is not set up yet.", message.Embeds[0].Title);
        await _serverService.DidNotReceive().UpdateAsync(Arg.Any<ServerUpdateDto>());
    }

    [TestMethod]
    public async Task ChangeAsync_WhenPartialOptionsProvided_UpdatesOnlyProvidedSettings()
    {
        var existing = new ServerReadDto(
            100,
            "Absalom",
            "Old description",
            111,
            222,
            new ServerSettings(
                ServerMode.Westmarch,
                GeneralColor: Color.Teal,
                ErrorColor: Color.DarkRed,
                SuccessColor: Color.DarkGreen,
                WarningColor: Color.Orange));
        _serverService.GetByIdAsync(100).Returns(existing);

        var message = await _sut.ChangeAsync(
            guildId: 100,
            gmRole: null,
            adminChannel: null,
            description: "Updated description",
            generalColor: "Purple",
            errorColor: null,
            successColor: null,
            warningColor: null);

        Assert.AreEqual("Success: Server settings have been updated.", message.Embeds[0].Title);
        await _serverService.Received(1).UpdateAsync(Arg.Is<ServerUpdateDto>(dto =>
            dto.Id == 100 &&
            dto.Name == "Absalom" &&
            dto.Description == "Updated description" &&
            dto.GMRoleId == 111 &&
            dto.AdminChannelId == 222 &&
            dto.Settings.Mode == ServerMode.Westmarch &&
            dto.Settings.GeneralColor == Color.Purple &&
            dto.Settings.ErrorColor == Color.DarkRed &&
            dto.Settings.SuccessColor == Color.DarkGreen &&
            dto.Settings.WarningColor == Color.Orange));
    }

    [TestMethod]
    public async Task ChangeAsync_WhenAllOptionsProvided_UpdatesRoleChannelDescriptionAndAllColors()
    {
        var existing = new ServerReadDto(
            100,
            "Absalom",
            null,
            111,
            222,
            new ServerSettings(ServerMode.Normal));
        _serverService.GetByIdAsync(100).Returns(existing);

        var newRole = CreateDiscordEntity<DiscordRole>(333);
        var newChannel = CreateDiscordEntity<DiscordChannel>(444);

        var message = await _sut.ChangeAsync(
            guildId: 100,
            gmRole: newRole,
            adminChannel: newChannel,
            description: "All settings changed",
            generalColor: "Blue",
            errorColor: "Red",
            successColor: "Green",
            warningColor: "Gold");

        Assert.AreEqual("Success: Server settings have been updated.", message.Embeds[0].Title);
        await _serverService.Received(1).UpdateAsync(Arg.Is<ServerUpdateDto>(dto =>
            dto.Id == 100 &&
            dto.Description == "All settings changed" &&
            dto.GMRoleId == 333 &&
            dto.AdminChannelId == 444 &&
            dto.Settings.Mode == ServerMode.Normal &&
            dto.Settings.GeneralColor == Color.Blue &&
            dto.Settings.ErrorColor == Color.Red &&
            dto.Settings.SuccessColor == Color.Green &&
            dto.Settings.WarningColor == Color.Gold));
    }

    [TestMethod]
    public async Task ChangeAsync_WhenServiceThrows_ReturnsErrorNotification()
    {
        var existing = new ServerReadDto(100, "Absalom", null, 111, 222, new ServerSettings());
        _serverService.GetByIdAsync(100).Returns(existing);
        _serverService.UpdateAsync(Arg.Any<ServerUpdateDto>()).ThrowsAsync(new InvalidOperationException("DB error"));

        var message = await _sut.ChangeAsync(
            guildId: 100,
            gmRole: null,
            adminChannel: null,
            description: "Will fail",
            generalColor: null,
            errorColor: null,
            successColor: null,
            warningColor: null);

        Assert.AreEqual("Error: Something went wrong while updating the server settings.", message.Embeds[0].Title);
    }

    [TestMethod]
    public async Task ChangeAsync_WhenInvalidColorProvided_ReturnsErrorNotification()
    {
        var existing = new ServerReadDto(100, "Absalom", null, 111, 222, new ServerSettings());
        _serverService.GetByIdAsync(100).Returns(existing);

        var message = await _sut.ChangeAsync(
            guildId: 100,
            gmRoleId: null,
            adminChannelId: null,
            description: null,
            generalColor: "NotAValidColor",
            errorColor: null,
            successColor: null,
            warningColor: null);

        Assert.AreEqual("Error: 'NotAValidColor' is not a valid color name.", message.Embeds[0].Title);
        await _serverService.DidNotReceive().UpdateAsync(Arg.Any<ServerUpdateDto>());
    }

    [TestMethod]
    public async Task ChangeAsync_WithUlongOverload_UpdatesServerSuccessfully()
    {
        var existing = new ServerReadDto(100, "Absalom", null, 111, 222, new ServerSettings());
        _serverService.GetByIdAsync(100).Returns(existing);

        var message = await _sut.ChangeAsync(
            guildId: 100,
            gmRoleId: 333,
            adminChannelId: 444,
            description: "Updated with ulong IDs",
            generalColor: "Teal",
            errorColor: null,
            successColor: null,
            warningColor: null);

        Assert.AreEqual("Success: Server settings have been updated.", message.Embeds[0].Title);
        await _serverService.Received(1).UpdateAsync(Arg.Is<ServerUpdateDto>(dto =>
            dto.Id == 100 &&
            dto.GMRoleId == 333 &&
            dto.AdminChannelId == 444 &&
            dto.Description == "Updated with ulong IDs"));
    }

    // ---- CreateChangeModal ----------------------------------------------

    [TestMethod]
    public void CreateChangeModal_ReturnsModalWithPrePopulatedServerValues()
    {
        var server = new ServerReadDto(
            Id: 100,
            Name: "Test Server",
            Description: "Current description",
            GMRoleId: 111,
            AdminChannelId: 222,
            Settings: new ServerSettings(
                Mode: ServerMode.Normal,
                GeneralColor: Color.Blurple,
                ErrorColor: Color.DarkRed,
                SuccessColor: Color.DarkGreen,
                WarningColor: Color.Gold));

        var modal = _sut.CreateChangeModal(server);

        Assert.AreEqual("Change Server Settings", modal.Title);
        Assert.AreEqual("serverChangeModal:0:0", modal.CustomId);
        Assert.HasCount(5, modal.Components);

        var descComp = (TextInputComponent)((DiscordActionRowComponent)modal.Components[0]).Components.First();
        Assert.AreEqual("server_description", descComp.CustomId);
        Assert.AreEqual("Current description", descComp.Value);

        var generalColorComp = (TextInputComponent)((DiscordActionRowComponent)modal.Components[1]).Components.First();
        Assert.AreEqual("server_general_color", generalColorComp.CustomId);
        Assert.AreEqual("Blurple", generalColorComp.Value);

        var errorColorComp = (TextInputComponent)((DiscordActionRowComponent)modal.Components[2]).Components.First();
        Assert.AreEqual("server_error_color", errorColorComp.CustomId);
        Assert.AreEqual("DarkRed", errorColorComp.Value);

        var successColorComp = (TextInputComponent)((DiscordActionRowComponent)modal.Components[3]).Components.First();
        Assert.AreEqual("server_success_color", successColorComp.CustomId);
        Assert.AreEqual("DarkGreen", successColorComp.Value);

        var warningColorComp = (TextInputComponent)((DiscordActionRowComponent)modal.Components[4]).Components.First();
        Assert.AreEqual("server_warning_color", warningColorComp.CustomId);
        Assert.AreEqual("Gold", warningColorComp.Value);
    }

    [TestMethod]
    public void CreateChangeModal_WithRoleAndChannel_EncodesIdsInCustomId()
    {
        var server = new ServerReadDto(
            Id: 100,
            Name: "Test Server",
            Description: null,
            GMRoleId: 111,
            AdminChannelId: 222,
            Settings: new ServerSettings());

        var modal = _sut.CreateChangeModal(server, gmRoleId: 999, adminChannelId: 888);

        Assert.AreEqual("serverChangeModal:999:888", modal.CustomId);
    }
}
