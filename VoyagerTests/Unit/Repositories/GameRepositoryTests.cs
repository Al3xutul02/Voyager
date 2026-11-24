using Repository.Context;
using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories;
using VoyagerTests.TestHelpers;

namespace VoyagerTests.Repositories;

/// <summary>
/// Unit tests for <see cref="GameRepository"/> and player enrollment operations,
/// run against the EF Core in-memory provider.
/// </summary>
[TestClass]
public class GameRepositoryTests
{
    private static Game MakeGame(
        ulong id,
        ulong serverId,
        ulong gameMasterId,
        string name,
        ulong categoryId = 1000,
        ulong gmChannelId = 1001,
        ulong generalChannelId = 1002,
        ulong charactersChannelId = 1003,
        ulong npcsChannelId = 1004,
        DateTime? deletedAt = null) =>
        new()
        {
            Id = id,
            ServerId = serverId,
            GameMasterId = gameMasterId,
            Name = name,
            Description = "Pathfinder 2e campaign",
            Color = 1,
            GmTokenImagePath = $"Games/{serverId}/{id}/gm_token.png",
            ThumbnailImagePath = $"Games/{serverId}/{id}/thumbnail.png",
            CategoryId = categoryId,
            GmChannelId = gmChannelId,
            GeneralChannelId = generalChannelId,
            CharactersChannelId = charactersChannelId,
            NpcsChannelId = npcsChannelId,
            DeletedAt = deletedAt
        };

    private static async Task<VoyagerDbContext> SeedAsync(params Game[] games)
    {
        var context = InMemoryDb.CreateContext();
        context.Games.AddRange(games);
        await context.SaveChangesAsync();
        return context;
    }

    // ---- GetByIdAsync (BaseRepository) ----------------------------------

    [TestMethod]
    public async Task GetByIdAsync_ReturnsMatchingGame()
    {
        await using var context = await SeedAsync(
            MakeGame(1, 100, 10, "Abomination Vaults"),
            MakeGame(2, 100, 10, "Kingmaker"));
        var repo = new GameRepository(context);

        var result = await repo.GetByIdAsync(2, IncludeBehavior.NoInclude);

        Assert.IsNotNull(result);
        Assert.AreEqual("Kingmaker", result!.Name);
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        await using var context = await SeedAsync(MakeGame(1, 100, 10, "Abomination Vaults"));
        var repo = new GameRepository(context);

        var result = await repo.GetByIdAsync(999, IncludeBehavior.NoInclude);

        Assert.IsNull(result);
    }

    // ---- GetByServerIdAsync ---------------------------------------------

    [TestMethod]
    public async Task GetByServerIdAsync_ReturnsOnlyActiveGamesForServer()
    {
        await using var context = await SeedAsync(
            MakeGame(1, 100, 10, "Game A"),
            MakeGame(2, 100, 20, "Game B"),
            MakeGame(3, 100, 10, "Deleted Game", deletedAt: DateTime.UtcNow),
            MakeGame(4, 200, 10, "Other Server Game"));
        var repo = new GameRepository(context);

        var results = (await repo.GetByServerIdAsync(100, IncludeBehavior.NoInclude)).ToList();

        Assert.HasCount(2, results);
        CollectionAssert.AreEquivalent(new[] { "Game A", "Game B" }, results.Select(g => g.Name).ToList());
    }

    // ---- GetByServerAndNameAsync ----------------------------------------

    [TestMethod]
    public async Task GetByServerAndNameAsync_ReturnsActiveMatchingGame()
    {
        await using var context = await SeedAsync(
            MakeGame(1, 100, 10, "Abomination Vaults"),
            MakeGame(2, 200, 20, "Abomination Vaults"),
            MakeGame(3, 100, 10, "Deleted Vaults", deletedAt: DateTime.UtcNow));
        var repo = new GameRepository(context);

        var active = await repo.GetByServerAndNameAsync(100, "Abomination Vaults", IncludeBehavior.NoInclude);
        Assert.IsNotNull(active);
        Assert.AreEqual(1UL, active!.Id);

        var deleted = await repo.GetByServerAndNameAsync(100, "Deleted Vaults", IncludeBehavior.NoInclude);
        Assert.IsNull(deleted);
    }

    // ---- GetByChannelIdAsync --------------------------------------------

    [TestMethod]
    public async Task GetByChannelIdAsync_MatchesCategoryOrAnyOfTheFourChannels()
    {
        await using var context = await SeedAsync(
            MakeGame(
                id: 1,
                serverId: 100,
                gameMasterId: 10,
                name: "Strength of Thousands",
                categoryId: 500,
                gmChannelId: 501,
                generalChannelId: 502,
                charactersChannelId: 503,
                npcsChannelId: 504));
        var repo = new GameRepository(context);

        foreach (var chId in new ulong[] { 500, 501, 502, 503, 504 })
        {
            var found = await repo.GetByChannelIdAsync(100, chId, IncludeBehavior.NoInclude);
            Assert.IsNotNull(found, $"Expected channel ID {chId} to resolve the game.");
            Assert.AreEqual(1UL, found!.Id);
        }

        var unknown = await repo.GetByChannelIdAsync(100, 999, IncludeBehavior.NoInclude);
        Assert.IsNull(unknown);
    }

    // ---- GetByUserIdAsync -----------------------------------------------

    [TestMethod]
    public async Task GetByUserIdAsync_ReturnsGamesWhereUserIsGmOrPlayer()
    {
        await using var context = InMemoryDb.CreateContext();
        context.Games.AddRange(
            MakeGame(1, 100, 10, "Hosted By 10"),
            MakeGame(2, 100, 20, "Joined By 10"),
            MakeGame(3, 100, 30, "Unrelated Game"),
            MakeGame(4, 100, 10, "Deleted Hosted", deletedAt: DateTime.UtcNow));
        context.GamePlayers.Add(new GamePlayer { GameId = 2, UserId = 10 });
        await context.SaveChangesAsync();

        var repo = new GameRepository(context);
        var results = (await repo.GetByUserIdAsync(10, IncludeBehavior.NoInclude)).ToList();

        Assert.HasCount(2, results);
        CollectionAssert.AreEquivalent(new[] { "Hosted By 10", "Joined By 10" }, results.Select(g => g.Name).ToList());
    }

    // ---- GetCountByServerIdAsync ----------------------------------------

    [TestMethod]
    public async Task GetCountByServerIdAsync_CountsOnlyActiveGamesForServer()
    {
        await using var context = await SeedAsync(
            MakeGame(1, 100, 10, "Active 1"),
            MakeGame(2, 100, 10, "Active 2"),
            MakeGame(3, 100, 10, "Deleted", deletedAt: DateTime.UtcNow),
            MakeGame(4, 999, 10, "Other Server"));
        var repo = new GameRepository(context);

        var count = await repo.GetCountByServerIdAsync(100);

        Assert.AreEqual(2, count);
    }

    // ---- AddPlayerAsync & IsPlayerInGameAsync ---------------------------

    [TestMethod]
    public async Task AddPlayerAsync_EnrollsPlayerAndIsPlayerInGameReturnsTrue()
    {
        await using var context = await SeedAsync(MakeGame(1, 100, 10, "Fists of the Ruby Phoenix"));
        var repo = new GameRepository(context);

        Assert.IsFalse(await repo.IsPlayerInGameAsync(1, 42));

        await repo.AddPlayerAsync(new GamePlayer { GameId = 1, UserId = 42 });
        await repo.SaveAsync();

        Assert.IsTrue(await repo.IsPlayerInGameAsync(1, 42));
        Assert.IsFalse(await repo.IsPlayerInGameAsync(1, 99));
    }

    // ---- CharacterRepository.UpdateCurrentGameAsync ---------------------

    [TestMethod]
    public async Task CharacterRepository_UpdateCurrentGameAsync_UpdatesOrClearsActiveCharacters()
    {
        await using var context = InMemoryDb.CreateContext();
        context.Characters.AddRange(
            new Character
            {
                Id = 1,
                UserId = 10,
                Name = "Sunaki",
                Color = 1,
                TokenImagePath = "t.png",
                ActorImagePath = "a.png",
                CurrentGame = "Old Campaign",
                Gold = 1m,
                Details = "{}"
            },
            new Character
            {
                Id = 2,
                UserId = 20,
                Name = "Valeros",
                Color = 1,
                TokenImagePath = "t2.png",
                ActorImagePath = "a2.png",
                CurrentGame = "Other Campaign",
                Gold = 1m,
                Details = "{}"
            });
        await context.SaveChangesAsync();

        var charRepo = new CharacterRepository(context);

        await charRepo.UpdateCurrentGameAsync("Old Campaign", "Renamed Campaign");
        await charRepo.SaveAsync();
        Assert.AreEqual("Renamed Campaign", (await charRepo.GetByIdAsync(1, IncludeBehavior.NoInclude))!.CurrentGame);
        Assert.AreEqual("Other Campaign", (await charRepo.GetByIdAsync(2, IncludeBehavior.NoInclude))!.CurrentGame);

        await charRepo.UpdateCurrentGameAsync("Renamed Campaign", null);
        await charRepo.SaveAsync();
        Assert.IsNull((await charRepo.GetByIdAsync(1, IncludeBehavior.NoInclude))!.CurrentGame);
    }
}
