using BusinessLogic.Enums.Types;
using BusinessLogic.Json;
using BusinessLogic.Json.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories;

namespace VoyagerTests.Integration;

/// <summary>
/// Game repository tests that run against a real MySQL instance (via Testcontainers).
/// Validates the <c>games</c> and <c>game_players</c> table migrations, foreign keys to
/// <c>servers</c> and <c>users</c>, channel snowflake lookups, and Pomelo LINQ-to-SQL translation.
/// </summary>
[TestClass]
[DoNotParallelize]
[TestCategory("Integration")]
public class GameRepositoryIntegrationTests
{
    private const ulong ServerSnowflake = 987654321012345678UL;
    private const ulong GmSnowflake = 111222333444555666UL;
    private const ulong PlayerSnowflake = 777888999000111222UL;

    private static string DefaultUserSettings() =>
        JsonConvert.SerializeObject(new UserSettings(Color.Teal), VoyagerJsonSettings.Default);

    private static string DefaultServerSettings() =>
        JsonConvert.SerializeObject(new ServerSettings(ServerMode.Normal), VoyagerJsonSettings.Default);

    [ClassInitialize]
    public static async Task StartContainer(TestContext _) => await MySqlContainerFixture.StartAsync();

    [ClassCleanup]
    public static async Task StopContainer() => await MySqlContainerFixture.StopAsync();

    [TestInitialize]
    public async Task ResetDatabase() => await MySqlContainerFixture.ResetAsync();

    private static async Task SeedServerAndUsersAsync()
    {
        await using var context = MySqlContainerFixture.CreateContext();
        var serverRepo = new ServerRepository(context);
        await serverRepo.AddAsync(new Server
        {
            Id = ServerSnowflake,
            Name = "Otari Hub",
            GMRoleId = 100,
            AdminChannelId = 200,
            Settings = DefaultServerSettings()
        });

        var userRepo = new UserRepository(context);
        await userRepo.AddAsync(new User { Id = GmSnowflake, Name = "GM_Ezren", Settings = DefaultUserSettings() });
        await userRepo.AddAsync(new User { Id = PlayerSnowflake, Name = "Kyra", Settings = DefaultUserSettings() });

        await context.SaveChangesAsync();
    }

    [TestMethod]
    public async Task AddAsync_AndAddPlayerAsync_RoundTripsThroughMySqlWithIncludes()
    {
        await SeedServerAndUsersAsync();
        ulong generatedGameId;

        await using (var writeContext = MySqlContainerFixture.CreateContext())
        {
            var gameRepo = new GameRepository(writeContext);
            var game = new Game
            {
                ServerId = ServerSnowflake,
                GameMasterId = GmSnowflake,
                Name = "Abomination Vaults",
                Description = "Explore Gauntlight Keep",
                Color = (int)Color.Purple,
                GmTokenImagePath = "Games/1/gm_token.png",
                ThumbnailImagePath = "Games/1/thumbnail.png",
                CategoryId = 5000,
                GmChannelId = 5001,
                GeneralChannelId = 5002,
                CharactersChannelId = 5003,
                NpcsChannelId = 5004
            };

            await gameRepo.AddAsync(game);
            await gameRepo.SaveAsync();
            generatedGameId = game.Id;

            await gameRepo.AddPlayerAsync(new GamePlayer
            {
                GameId = generatedGameId,
                UserId = PlayerSnowflake
            });
            await gameRepo.SaveAsync();
        }

        Assert.IsGreaterThan(0UL, generatedGameId);

        await using var readContext = MySqlContainerFixture.CreateContext();
        var readRepo = new GameRepository(readContext);

        var loaded = await readRepo.GetByIdAsync(
            generatedGameId,
            IncludeBehavior.GivenIncludes,
            q => q.Include(g => g.GameMaster).Include(g => g.Players).ThenInclude(p => p.User));

        Assert.IsNotNull(loaded);
        Assert.AreEqual("Abomination Vaults", loaded!.Name);
        Assert.AreEqual("GM_Ezren", loaded.GameMaster.Name);
        Assert.HasCount(1, loaded.Players);
        Assert.AreEqual("Kyra", loaded.Players.First().User.Name);

        Assert.IsTrue(await readRepo.IsPlayerInGameAsync(generatedGameId, PlayerSnowflake));
        Assert.AreEqual(1, await readRepo.GetCountByServerIdAsync(ServerSnowflake));
    }

    [TestMethod]
    public async Task Queries_ByChannelByUserAndByName_TranslateToMySql()
    {
        await SeedServerAndUsersAsync();

        await using (var writeContext = MySqlContainerFixture.CreateContext())
        {
            var gameRepo = new GameRepository(writeContext);
            var game = new Game
            {
                ServerId = ServerSnowflake,
                GameMasterId = GmSnowflake,
                Name = "Kingmaker",
                Description = "Stolen Lands",
                Color = (int)Color.Gold,
                GmTokenImagePath = "gm.png",
                ThumbnailImagePath = "thumb.png",
                CategoryId = 6000,
                GmChannelId = 6001,
                GeneralChannelId = 6002,
                CharactersChannelId = 6003,
                NpcsChannelId = 6004
            };
            await gameRepo.AddAsync(game);
            await gameRepo.SaveAsync();

            await gameRepo.AddPlayerAsync(new GamePlayer { GameId = game.Id, UserId = PlayerSnowflake });
            await gameRepo.SaveAsync();
        }

        await using var readContext = MySqlContainerFixture.CreateContext();
        var repo = new GameRepository(readContext);

        var byChannel = await repo.GetByChannelIdAsync(ServerSnowflake, 6002, IncludeBehavior.NoInclude);
        Assert.IsNotNull(byChannel);
        Assert.AreEqual("Kingmaker", byChannel!.Name);

        var byName = await repo.GetByServerAndNameAsync(ServerSnowflake, "Kingmaker", IncludeBehavior.NoInclude);
        Assert.IsNotNull(byName);

        var gmGames = (await repo.GetByUserIdAsync(GmSnowflake, IncludeBehavior.NoInclude)).ToList();
        Assert.HasCount(1, gmGames);

        var playerGames = (await repo.GetByUserIdAsync(PlayerSnowflake, IncludeBehavior.NoInclude)).ToList();
        Assert.HasCount(1, playerGames);
    }
}
