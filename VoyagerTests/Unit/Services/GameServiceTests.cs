using AutoMapper;
using BusinessLogic.Dtos.Game;
using BusinessLogic.Enums.Types;
using BusinessLogic.Services;
using NSubstitute;
using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories.Abstractions;
using VoyagerTests.TestHelpers;

namespace VoyagerTests.Services;

/// <summary>
/// Unit tests for <see cref="GameService"/> covering CRUD operations,
/// player enrollment, character CurrentGame synchronization, and soft-delete filtering.
/// </summary>
[TestClass]
public class GameServiceTests
{
    private readonly IGameRepository _gameRepository = Substitute.For<IGameRepository>();
    private readonly ICharacterRepository _characterRepository = Substitute.For<ICharacterRepository>();
    private readonly IMapper _mapper = MapperHelper.CreateMapper();
    private GameService _sut = null!;

    private static Game MakeGame(
        ulong id = 1,
        ulong serverId = 100,
        ulong gmId = 10,
        string name = "Abomination Vaults",
        DateTime? deletedAt = null) =>
        new()
        {
            Id = id,
            ServerId = serverId,
            GameMasterId = gmId,
            GameMaster = new User { Id = gmId, Name = "GM_Ezren", Settings = "{}" },
            Name = name,
            Description = "Megadungeon adventure",
            Color = (int)Color.Purple,
            GmTokenImagePath = "Games/100/1/gm_token.png",
            ThumbnailImagePath = "Games/100/1/thumbnail.png",
            CategoryId = 500,
            GmChannelId = 501,
            GeneralChannelId = 502,
            CharactersChannelId = 503,
            NpcsChannelId = 504,
            DeletedAt = deletedAt,
            Players =
            [
                new GamePlayer
                {
                    GameId = id,
                    UserId = 42,
                    User = new User { Id = 42, Name = "Kyra", Settings = "{}" }
                }
            ]
        };

    [TestInitialize]
    public void Setup() => _sut = new GameService(_mapper, _gameRepository, _characterRepository);

    [TestMethod]
    public async Task GetByIdAsync_WhenActive_ReturnsMappedDtoWithGmAndPlayers()
    {
        var game = MakeGame();
        _gameRepository.GetByIdAsync(1, IncludeBehavior.GivenIncludes, Arg.Any<Func<IQueryable<Game>, IQueryable<Game>>>())
            .Returns(game);

        var result = await _sut.GetByIdAsync(1);

        Assert.IsNotNull(result);
        Assert.AreEqual("Abomination Vaults", result!.Name);
        Assert.AreEqual("GM_Ezren", result.GameMasterName);
        Assert.AreEqual(Color.Purple, result.Color);
        Assert.HasCount(1, result.Players);
        Assert.AreEqual("Kyra", result.Players[0].Name);
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenSoftDeleted_ReturnsNull()
    {
        var game = MakeGame(deletedAt: DateTime.UtcNow);
        _gameRepository.GetByIdAsync(1, IncludeBehavior.GivenIncludes, Arg.Any<Func<IQueryable<Game>, IQueryable<Game>>>())
            .Returns(game);

        var result = await _sut.GetByIdAsync(1);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetByServerIdAsync_FiltersOutSoftDeletedGames()
    {
        var games = new List<Game>
        {
            MakeGame(1, 100, 10, "Active Game"),
            MakeGame(2, 100, 10, "Deleted Game", deletedAt: DateTime.UtcNow)
        };
        _gameRepository.GetByServerIdAsync(100, IncludeBehavior.GivenIncludes, Arg.Any<Func<IQueryable<Game>, IQueryable<Game>>>())
            .Returns(games);

        var results = (await _sut.GetByServerIdAsync(100)).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("Active Game", results[0].Name);
    }

    [TestMethod]
    public async Task GetByServerAndNameAsync_ReturnsMappedDto_WhenActive()
    {
        var game = MakeGame(1, 100, 10, "Kingmaker");
        _gameRepository.GetByServerAndNameAsync(100, "Kingmaker", IncludeBehavior.GivenIncludes, Arg.Any<Func<IQueryable<Game>, IQueryable<Game>>>())
            .Returns(game);

        var result = await _sut.GetByServerAndNameAsync(100, "Kingmaker");

        Assert.IsNotNull(result);
        Assert.AreEqual("Kingmaker", result!.Name);
    }

    [TestMethod]
    public async Task GetByChannelIdAsync_ReturnsMappedDto_WhenActive()
    {
        var game = MakeGame(1, 100, 10, "Kingmaker");
        _gameRepository.GetByChannelIdAsync(100, 501, IncludeBehavior.GivenIncludes, Arg.Any<Func<IQueryable<Game>, IQueryable<Game>>>())
            .Returns(game);

        var result = await _sut.GetByChannelIdAsync(100, 501);

        Assert.IsNotNull(result);
        Assert.AreEqual("Kingmaker", result!.Name);
    }

    [TestMethod]
    public async Task GetByUserIdAsync_ReturnsActiveUserGames()
    {
        var games = new List<Game> { MakeGame(1, 100, 10, "Campaign 1") };
        _gameRepository.GetByUserIdAsync(10, IncludeBehavior.GivenIncludes, Arg.Any<Func<IQueryable<Game>, IQueryable<Game>>>())
            .Returns(games);

        var results = (await _sut.GetByUserIdAsync(10)).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("Campaign 1", results[0].Name);
    }

    [TestMethod]
    public async Task AddPlayerAsync_WhenNotAlreadyInGame_AddsAndSaves()
    {
        _gameRepository.IsPlayerInGameAsync(1, 77).Returns(false);

        await _sut.AddPlayerAsync(1, 77);

        await _gameRepository.Received(1).AddPlayerAsync(Arg.Is<GamePlayer>(gp =>
            gp.GameId == 1 && gp.UserId == 77));
        await _gameRepository.Received(1).SaveAsync();
    }

    [TestMethod]
    public async Task AddPlayerAsync_WhenAlreadyInGame_DoesNotAddAgain()
    {
        _gameRepository.IsPlayerInGameAsync(1, 77).Returns(true);

        await _sut.AddPlayerAsync(1, 77);

        await _gameRepository.DidNotReceive().AddPlayerAsync(Arg.Any<GamePlayer>());
        await _gameRepository.DidNotReceive().SaveAsync();
    }

    [TestMethod]
    public async Task UpdateAsync_WhenRenamed_UpdatesCharacterCurrentGame()
    {
        var existing = MakeGame(1, 100, 10, "Old Name");
        _gameRepository.GetByIdAsync(1, IncludeBehavior.NoInclude).Returns(existing);

        var updateDto = new GameUpdateDto(
            Id: 1,
            ServerId: 100,
            GameMasterId: 10,
            Name: "New Name",
            Description: "Updated",
            Color: Color.Gold,
            GmTokenImagePath: "gm.png",
            ThumbnailImagePath: "thumb.png",
            CategoryId: 500,
            GmChannelId: 501,
            GeneralChannelId: 502,
            CharactersChannelId: 503,
            NpcsChannelId: 504);

        await _sut.UpdateAsync(updateDto);

        _gameRepository.Received(1).Update(Arg.Is<Game>(g => g.Id == 1 && g.Name == "New Name"));
        await _characterRepository.Received(1).UpdateCurrentGameAsync("Old Name", "New Name");
        await _gameRepository.Received(1).SaveAsync();
    }

    [TestMethod]
    public async Task DeleteAsync_SoftDeletesGameAndClearsCharacterCurrentGame()
    {
        var existing = MakeGame(1, 100, 10, "Abomination Vaults");
        _gameRepository.GetByIdAsync(1, IncludeBehavior.NoInclude).Returns(existing);

        await _sut.DeleteAsync(1);

        Assert.IsNotNull(existing.DeletedAt);
        _gameRepository.Received(1).Update(existing);
        await _characterRepository.Received(1).UpdateCurrentGameAsync("Abomination Vaults", null);
        await _gameRepository.Received(1).SaveAsync();
    }
}
