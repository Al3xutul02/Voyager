using System.Text;
using BusinessLogic.Dtos.Game;
using BusinessLogic.Dtos.Server;
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
/// Unit tests for <see cref="GameHandler"/> covering <c>/game create</c>, <c>/game edit</c>,
/// <c>/game delete</c>, <c>/game details</c>, <c>/game invite</c>, and button interactions.
/// </summary>
[TestClass]
public class GameHandlerTests
{
    private readonly IGameService _gameService = Substitute.For<IGameService>();
    private readonly IServerService _serverService = Substitute.For<IServerService>();
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly IFileService _fileService = Substitute.For<IFileService>();
    private readonly IEnumService _enumService = new EnumService();
    private GameHandler _sut = null!;

    private static T CreateDiscordEntity<T>(ulong id) where T : SnowflakeObject
    {
        var entity = (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;
        typeof(SnowflakeObject).GetProperty(nameof(SnowflakeObject.Id))!
            .SetValue(entity, id);
        return entity;
    }

    private static ServerReadDto MakeServer(
        ulong id = 100,
        ServerMode mode = ServerMode.Normal,
        ulong gmRoleId = 200) =>
        new(id, "Golarion Server", "A PbP server", gmRoleId, 300, new ServerSettings(mode));

    private static GameReadDto MakeGame(
        ulong id = 1,
        ulong serverId = 100,
        ulong gmId = 10,
        string name = "Abomination Vaults",
        ulong gmChannelId = 501,
        IReadOnlyList<UserReadDto>? players = null) =>
        new(
            Id: id,
            ServerId: serverId,
            GameMasterId: gmId,
            GameMasterName: "GM_Ezren",
            Name: name,
            Description: "A deep dungeon crawl",
            Color: Color.Purple,
            GmTokenImagePath: "Games/100/1/gm_token.png",
            ThumbnailImagePath: "Games/100/1/thumbnail.png",
            CategoryId: 500,
            GmChannelId: gmChannelId,
            GeneralChannelId: 502,
            CharactersChannelId: 503,
            NpcsChannelId: 504,
            Players: players ?? []);

    [TestInitialize]
    public void Setup()
    {
        var viewFactory = new ViewFactory(true, _enumService, new ServerSettings());
        _sut = new GameHandler(
            _gameService,
            _serverService,
            _userService,
            _fileService,
            _enumService,
            viewFactory);
    }

    // ---- Permission helpers ---------------------------------------------

    [TestMethod]
    public void IsGmOrAdmin_EvaluatesRoleAdminAndOwnerCorrectly()
    {
        Assert.IsTrue(GameHandler.IsGmOrAdmin(isServerOwner: true, isAdmin: false, gmRoleId: 200, callerRoleIds: null));
        Assert.IsTrue(GameHandler.IsGmOrAdmin(isServerOwner: false, isAdmin: true, gmRoleId: 200, callerRoleIds: null));
        Assert.IsTrue(GameHandler.IsGmOrAdmin(isServerOwner: false, isAdmin: false, gmRoleId: 200, callerRoleIds: [100, 200]));
        Assert.IsFalse(GameHandler.IsGmOrAdmin(isServerOwner: false, isAdmin: false, gmRoleId: 200, callerRoleIds: [100]));
        Assert.IsFalse(GameHandler.IsGmOrAdmin(isServerOwner: false, isAdmin: false, gmRoleId: 200, callerRoleIds: null));
    }

    [TestMethod]
    public void CanManageGame_AllowsGameCreatorOrAdminOrOwner()
    {
        Assert.IsTrue(GameHandler.CanManageGame(callerId: 10, gameMasterId: 10, isServerOwner: false, isAdmin: false));
        Assert.IsTrue(GameHandler.CanManageGame(callerId: 99, gameMasterId: 10, isServerOwner: true, isAdmin: false));
        Assert.IsTrue(GameHandler.CanManageGame(callerId: 99, gameMasterId: 10, isServerOwner: false, isAdmin: true));
        Assert.IsFalse(GameHandler.CanManageGame(callerId: 99, gameMasterId: 10, isServerOwner: false, isAdmin: false));
    }

    // ---- CreateAsync ----------------------------------------------------

    [TestMethod]
    public async Task CreateAsync_WhenServerNotSetUp_ReturnsError()
    {
        var guild = CreateDiscordEntity<DiscordGuild>(100);
        _serverService.GetByIdAsync(100).Returns((ServerReadDto?)null);

        var message = await _sut.CreateAsync(
            guild, 10, false, false, [200], "Vaults", "Desc", "Teal", "http://a/1.png", "1.png", "http://a/2.png", "2.png");

        Assert.AreEqual("Error: This server is not set up yet.", message.Embeds[0].Title);
    }

    [TestMethod]
    public async Task CreateAsync_WhenServerIsWestmarch_ReturnsError()
    {
        var guild = CreateDiscordEntity<DiscordGuild>(100);
        _serverService.GetByIdAsync(100).Returns(MakeServer(mode: ServerMode.Westmarch));

        var message = await _sut.CreateAsync(
            guild, 10, true, true, [200], "Vaults", "Desc", "Teal", "http://a/1.png", "1.png", "http://a/2.png", "2.png");

        StringAssert.Contains(message.Embeds[0].Title, "Normal mode");
    }

    [TestMethod]
    public async Task CreateAsync_WhenCallerNotGmOrAdmin_ReturnsError()
    {
        var guild = CreateDiscordEntity<DiscordGuild>(100);
        _serverService.GetByIdAsync(100).Returns(MakeServer());

        var message = await _sut.CreateAsync(
            guild, 10, false, false, [999], "Vaults", "Desc", "Teal", "http://a/1.png", "1.png", "http://a/2.png", "2.png");

        StringAssert.Contains(message.Embeds[0].Title, "Only Game Masters or server administrators");
    }

    [TestMethod]
    public async Task CreateAsync_WhenCallerHasNoProfile_ReturnsError()
    {
        var guild = CreateDiscordEntity<DiscordGuild>(100);
        _serverService.GetByIdAsync(100).Returns(MakeServer());
        _userService.GetByIdAsync(10).Returns((UserReadDto?)null);

        var message = await _sut.CreateAsync(
            guild, 10, false, false, [200], "Vaults", "Desc", "Teal", "http://a/1.png", "1.png", "http://a/2.png", "2.png");

        StringAssert.Contains(message.Embeds[0].Title, "create a profile first");
    }

    [TestMethod]
    public async Task CreateAsync_WhenServerReached5GamesLimit_ReturnsError()
    {
        var guild = CreateDiscordEntity<DiscordGuild>(100);
        _serverService.GetByIdAsync(100).Returns(MakeServer());
        _userService.GetByIdAsync(10).Returns(new UserReadDto(10, "GM", new UserSettings()));
        _gameService.GetCountByServerIdAsync(100).Returns(5);

        var message = await _sut.CreateAsync(
            guild, 10, false, false, [200], "Vaults", "Desc", "Teal", "http://a/1.png", "1.png", "http://a/2.png", "2.png");

        StringAssert.Contains(message.Embeds[0].Title, "maximum limit of 5 active games");
    }

    [TestMethod]
    public async Task CreateAsync_WhenGameNameAlreadyExists_ReturnsWarning()
    {
        var guild = CreateDiscordEntity<DiscordGuild>(100);
        _serverService.GetByIdAsync(100).Returns(MakeServer());
        _userService.GetByIdAsync(10).Returns(new UserReadDto(10, "GM", new UserSettings()));
        _gameService.GetCountByServerIdAsync(100).Returns(1);
        _gameService.GetByServerAndNameAsync(100, "Vaults").Returns(MakeGame(name: "Vaults"));

        var message = await _sut.CreateAsync(
            guild, 10, false, false, [200], "Vaults", "Desc", "Teal", "http://a/1.png", "1.png", "http://a/2.png", "2.png");

        StringAssert.Contains(message.Embeds[0].Title, "already exists on this server");
    }

    [TestMethod]
    public async Task CreateAsync_WhenInvalidColor_ReturnsError()
    {
        var guild = CreateDiscordEntity<DiscordGuild>(100);
        _serverService.GetByIdAsync(100).Returns(MakeServer());
        _userService.GetByIdAsync(10).Returns(new UserReadDto(10, "GM", new UserSettings()));

        var message = await _sut.CreateAsync(
            guild, 10, false, false, [200], "Vaults", "Desc", "InvalidColor", "http://a/1.png", "1.png", "http://a/2.png", "2.png");

        StringAssert.Contains(message.Embeds[0].Title, "not a valid color name");
    }

    // ---- EditAsync ------------------------------------------------------

    [TestMethod]
    public async Task EditAsync_WhenNoOptionalFieldsProvided_ReturnsWarning()
    {
        var guild = CreateDiscordEntity<DiscordGuild>(100);
        _serverService.GetByIdAsync(100).Returns(MakeServer());

        var message = await _sut.EditAsync(guild, 10, false, false, [200], "Abomination Vaults");

        Assert.AreEqual("Warning: No fields were provided to update.", message.Embeds[0].Title);
    }

    [TestMethod]
    public async Task EditAsync_WhenCallerIsDifferentGmAndNotAdmin_ReturnsError()
    {
        var guild = CreateDiscordEntity<DiscordGuild>(100);
        _serverService.GetByIdAsync(100).Returns(MakeServer());
        _gameService.GetByServerAndNameAsync(100, "Abomination Vaults").Returns(MakeGame(gmId: 10));

        var message = await _sut.EditAsync(
            guild,
            callerId: 99,
            isServerOwner: false,
            isAdmin: false,
            callerRoleIds: [200],
            name: "Abomination Vaults",
            description: "Attempted hijack");

        StringAssert.Contains(message.Embeds[0].Title, "only edit games that you created yourself");
        await _gameService.DidNotReceive().UpdateAsync(Arg.Any<GameUpdateDto>());
    }

    [TestMethod]
    public async Task EditAsync_WhenValidByCreatorGm_UpdatesGameFieldsAndImages()
    {
        var guild = CreateDiscordEntity<DiscordGuild>(100);
        _serverService.GetByIdAsync(100).Returns(MakeServer());
        _gameService.GetByServerAndNameAsync(100, "Abomination Vaults").Returns(MakeGame(id: 1, gmId: 10));
        _fileService.SaveAttachmentAsync("http://a/new_thumb.png", Arg.Any<string>(), Arg.Any<string>())
            .Returns("Games/100/1/thumbnail_new.png");

        var message = await _sut.EditAsync(
            guild,
            callerId: 10,
            isServerOwner: false,
            isAdmin: false,
            callerRoleIds: [200],
            name: "Abomination Vaults",
            description: "Updated description",
            color: "Gold",
            thumbnailUrl: "http://a/new_thumb.png",
            thumbnailFileName: "new_thumb.png");

        await _gameService.Received(1).UpdateAsync(Arg.Is<GameUpdateDto>(dto =>
            dto.Id == 1 &&
            dto.Description == "Updated description" &&
            dto.Color == Color.Gold &&
            dto.ThumbnailImagePath == "Games/100/1/thumbnail_new.png"));
        _fileService.Received(1).DeleteFile("Games/100/1/thumbnail.png");
        Assert.AreEqual("Game 'Abomination Vaults' updated successfully.", message.Embeds[0].Title);
    }

    [TestMethod]
    public async Task EditAsync_WhenAdminEditsOtherGmsGame_Succeeds()
    {
        var guild = CreateDiscordEntity<DiscordGuild>(100);
        _serverService.GetByIdAsync(100).Returns(MakeServer());
        _gameService.GetByServerAndNameAsync(100, "Abomination Vaults").Returns(MakeGame(id: 1, gmId: 10));

        var message = await _sut.EditAsync(
            guild,
            callerId: 999,
            isServerOwner: false,
            isAdmin: true,
            callerRoleIds: [],
            name: "Abomination Vaults",
            description: "Admin edit");

        await _gameService.Received(1).UpdateAsync(Arg.Is<GameUpdateDto>(dto =>
            dto.Id == 1 && dto.Description == "Admin edit"));
        Assert.AreEqual("Game 'Abomination Vaults' updated successfully.", message.Embeds[0].Title);
    }

    // ---- DeletePromptAsync & ConfirmDeleteAsync -------------------------

    [TestMethod]
    public async Task DeletePromptAsync_WhenCallerIsDifferentGmAndNotAdmin_ReturnsError()
    {
        _serverService.GetByIdAsync(100).Returns(MakeServer());
        _gameService.GetByServerAndNameAsync(100, "Abomination Vaults").Returns(MakeGame(gmId: 10));

        var message = await _sut.DeletePromptAsync(100, callerId: 99, isServerOwner: false, isAdmin: false, callerRoleIds: [200], name: "Abomination Vaults");

        StringAssert.Contains(message.Embeds[0].Title, "only delete games that you created yourself");
    }

    [TestMethod]
    public async Task DeletePromptAsync_WhenAuthorized_ReturnsConfirmationEmbedWithButtons()
    {
        _serverService.GetByIdAsync(100).Returns(MakeServer());
        _gameService.GetByServerAndNameAsync(100, "Abomination Vaults").Returns(MakeGame(id: 7, gmId: 10));

        var message = await _sut.DeletePromptAsync(100, callerId: 10, isServerOwner: false, isAdmin: false, callerRoleIds: [200], name: "Abomination Vaults");

        Assert.AreEqual("Delete Game: Abomination Vaults", message.Embeds[0].Title);
        StringAssert.Contains(message.Embeds[0].Description, "irreversible");
        StringAssert.Contains(message.Embeds[0].Description, "Characters associated with the game will **not** be affected");
        StringAssert.Contains(message.Embeds[0].Description, "archived");
        Assert.HasCount(1, message.Components);
        Assert.HasCount(2, message.Components[0].Components);
    }

    [TestMethod]
    public async Task ConfirmDeleteAsync_WhenUnauthorizedCaller_ReturnsError()
    {
        var message = await _sut.ConfirmDeleteAsync(null, callerId: 99, gameId: 1, authorizedUserId: 10);

        StringAssert.Contains(message.Embeds[0].Title, "not authorized");
        await _gameService.DidNotReceive().DeleteAsync(Arg.Any<ulong>());
    }

    [TestMethod]
    public async Task ConfirmDeleteAsync_WhenAuthorized_DeletesGameAndImageDirectory()
    {
        _gameService.GetByIdAsync(1).Returns(MakeGame(id: 1, gmId: 10));
        _serverService.GetByIdAsync(100).Returns(MakeServer());

        var message = await _sut.ConfirmDeleteAsync(null, callerId: 10, gameId: 1, authorizedUserId: 10);

        await _gameService.Received(1).DeleteAsync(1);
        _fileService.Received(1).DeleteDirectory("Games/100/1");
        StringAssert.Contains(message.Embeds[0].Title, "was deleted and its channels have been archived");
    }

    // ---- DetailsAsync ---------------------------------------------------

    [TestMethod]
    public async Task DetailsAsync_WithName_ReturnsGameDetailsEmbedWithThumbnailAndPlayers()
    {
        _serverService.GetByIdAsync(100).Returns(MakeServer());
        var players = new List<UserReadDto>
        {
            new(42, "Kyra", new UserSettings()),
            new(43, "Merisiel", new UserSettings())
        };
        var game = MakeGame(players: players);
        _gameService.GetByServerAndNameAsync(100, "Abomination Vaults").Returns(game);
        _fileService.OpenRead(game.ThumbnailImagePath).Returns(new MemoryStream(Encoding.UTF8.GetBytes("thumb")));

        var message = await _sut.DetailsAsync(100, channelId: 999, name: "Abomination Vaults");

        Assert.HasCount(1, message.Embeds);
        var embed = message.Embeds[0];
        Assert.AreEqual("Abomination Vaults", embed.Title);
        Assert.AreEqual("A deep dungeon crawl", embed.Description);
        Assert.AreEqual("GM_Ezren", embed.Fields.Single(f => f.Name == "Game Master").Value);
        var playersField = embed.Fields.Single(f => f.Name == "Players").Value;
        StringAssert.Contains(playersField, "• Kyra");
        StringAssert.Contains(playersField, "• Merisiel");
        Assert.HasCount(1, message.Files);
    }

    [TestMethod]
    public async Task DetailsAsync_WithoutName_ResolvesFromCurrentChannelId()
    {
        _serverService.GetByIdAsync(100).Returns(MakeServer());
        var game = MakeGame();
        _gameService.GetByChannelIdAsync(100, 502).Returns(game);

        var message = await _sut.DetailsAsync(100, channelId: 502, name: null);

        Assert.HasCount(1, message.Embeds);
        Assert.AreEqual("Abomination Vaults", message.Embeds[0].Title);
    }

    [TestMethod]
    public async Task DetailsAsync_WithoutNameInNonGameChannel_ReturnsError()
    {
        _serverService.GetByIdAsync(100).Returns(MakeServer());
        _gameService.GetByChannelIdAsync(100, 999).Returns((GameReadDto?)null);

        var message = await _sut.DetailsAsync(100, channelId: 999, name: null);

        StringAssert.Contains(message.Embeds[0].Title, "not part of any active game");
    }

    // ---- InviteAsync & HandleInviteResponseAsync ------------------------

    [TestMethod]
    public async Task InviteAsync_WhenNotInGmChannel_ReturnsError()
    {
        _serverService.GetByIdAsync(100).Returns(MakeServer());
        _gameService.GetByChannelIdAsync(100, 502).Returns(MakeGame(gmChannelId: 501)); // 502 is general, not gm-channel

        var message = await _sut.InviteAsync(100, 502, 10, 42, "Kyra", _ => Task.CompletedTask);

        StringAssert.Contains(message.Embeds[0].Title, "can only be used inside a game's gm-channel");
    }

    [TestMethod]
    public async Task InviteAsync_WhenCallerIsNotGameMaster_ReturnsError()
    {
        _serverService.GetByIdAsync(100).Returns(MakeServer());
        _gameService.GetByChannelIdAsync(100, 501).Returns(MakeGame(gmId: 10, gmChannelId: 501));

        var message = await _sut.InviteAsync(100, 501, callerId: 99, 42, "Kyra", _ => Task.CompletedTask);

        StringAssert.Contains(message.Embeds[0].Title, "Only the Game Master of this game can invite players");
    }

    [TestMethod]
    public async Task InviteAsync_WhenValid_SendsDmWithInviteEmbedAndButtons()
    {
        _serverService.GetByIdAsync(100).Returns(MakeServer());
        _gameService.GetByChannelIdAsync(100, 501).Returns(MakeGame(id: 1, gmId: 10, gmChannelId: 501));
        _userService.GetByIdAsync(42).Returns(new UserReadDto(42, "Kyra", new UserSettings()));
        _gameService.IsPlayerInGameAsync(1, 42).Returns(false);

        DiscordMessageBuilder? sentDm = null;
        var message = await _sut.InviteAsync(
            100,
            501,
            callerId: 10,
            targetUserId: 42,
            targetUsername: "Kyra",
            sendDmAsync: dm =>
            {
                sentDm = dm;
                return Task.CompletedTask;
            });

        Assert.IsNotNull(sentDm);
        Assert.AreEqual("Abomination Vaults", sentDm!.Embeds[0].Title);
        Assert.HasCount(1, sentDm.Components);
        Assert.HasCount(2, sentDm.Components[0].Components);
        StringAssert.Contains(message.Embeds[0].Title, "Invitation to join 'Abomination Vaults' has been sent to Kyra");
    }

    [TestMethod]
    public async Task HandleInviteResponseAsync_WhenAccepted_AddsPlayerAndReturnsDmAndGmNotifications()
    {
        _gameService.GetByIdAsync(1).Returns(MakeGame(id: 1, gmId: 10, gmChannelId: 501));
        _serverService.GetByIdAsync(100).Returns(MakeServer());
        _userService.GetByIdAsync(42).Returns(new UserReadDto(42, "Kyra", new UserSettings()));
        _gameService.IsPlayerInGameAsync(1, 42).Returns(false);

        var (dmResponse, gmChannelId, gmNotification) = await _sut.HandleInviteResponseAsync(
            callerId: 42,
            callerUsername: "Kyra",
            gameId: 1,
            invitedUserId: 42,
            accepted: true);

        await _gameService.Received(1).AddPlayerAsync(1, 42);
        StringAssert.Contains(dmResponse.Embeds[0].Title, "accepted the invitation");
        Assert.AreEqual(501UL, gmChannelId);
        Assert.IsNotNull(gmNotification);
        StringAssert.Contains(gmNotification!.Embeds[0].Title, "Accepted: Kyra");
    }

    [TestMethod]
    public async Task HandleInviteResponseAsync_WhenDeclined_DoesNotAddPlayerAndReturnsDeclinedNotifications()
    {
        _gameService.GetByIdAsync(1).Returns(MakeGame(id: 1, gmId: 10, gmChannelId: 501));
        _serverService.GetByIdAsync(100).Returns(MakeServer());
        _userService.GetByIdAsync(42).Returns(new UserReadDto(42, "Kyra", new UserSettings()));

        var (dmResponse, gmChannelId, gmNotification) = await _sut.HandleInviteResponseAsync(
            callerId: 42,
            callerUsername: "Kyra",
            gameId: 1,
            invitedUserId: 42,
            accepted: false);

        await _gameService.DidNotReceive().AddPlayerAsync(Arg.Any<ulong>(), Arg.Any<ulong>());
        StringAssert.Contains(dmResponse.Embeds[0].Title, "declined the invitation");
        Assert.AreEqual(501UL, gmChannelId);
        Assert.IsNotNull(gmNotification);
        StringAssert.Contains(gmNotification!.Embeds[0].Title, "Declined: Kyra");
    }
}
