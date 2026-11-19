using BusinessLogic.Enums.Types;
using BusinessLogic.Json;
using BusinessLogic.Json.Models;
using Newtonsoft.Json;
using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories;

namespace VoyagerTests.Integration;

/// <summary>
/// Server repository tests that run against a real MySQL instance (via Testcontainers).
/// Validates the <c>servers</c> table migration, Pomelo LINQ-to-SQL translation,
/// <c>ulong</c> (<c>BIGINT UNSIGNED</c>) columns (<c>Id</c>, <c>GMRoleId</c>,
/// <c>AdminChannelId</c>), nullable <c>Description</c>, and the <c>text</c>
/// <c>Settings</c> column.
/// </summary>
[TestClass]
[DoNotParallelize]
[TestCategory("Integration")]
public class ServerRepositoryIntegrationTests
{
    private const ulong GuildSnowflake = 1320120043694194732UL;
    private const ulong GmRoleSnowflake = 1320120043694194733UL;
    private const ulong AdminChannelSnowflake = 1320120043694194734UL;

    private static string Settings(ServerMode mode = ServerMode.Normal, Color generalColor = Color.Teal) =>
        JsonConvert.SerializeObject(new ServerSettings(mode, generalColor), VoyagerJsonSettings.Default);

    [ClassInitialize]
    public static async Task StartContainer(TestContext _) => await MySqlContainerFixture.StartAsync();

    [ClassCleanup]
    public static async Task StopContainer() => await MySqlContainerFixture.StopAsync();

    [TestInitialize]
    public async Task ResetDatabase() => await MySqlContainerFixture.ResetAsync();

    [TestMethod]
    public async Task AddAsync_RealSnowflakeIds_RoundTripsThroughMySql()
    {
        await using (var writeContext = MySqlContainerFixture.CreateContext())
        {
            var repo = new ServerRepository(writeContext);
            await repo.AddAsync(new Server
            {
                Id = GuildSnowflake,
                Name = "Golarion",
                Description = "Pathfinder 2E Westmarch",
                GMRoleId = GmRoleSnowflake,
                AdminChannelId = AdminChannelSnowflake,
                Settings = Settings(ServerMode.Westmarch, Color.Gold)
            });
            await repo.SaveAsync();
        }

        await using var readContext = MySqlContainerFixture.CreateContext();
        var stored = await new ServerRepository(readContext)
            .GetByIdAsync(GuildSnowflake, IncludeBehavior.NoInclude);

        Assert.IsNotNull(stored);
        Assert.AreEqual(GuildSnowflake, stored!.Id);
        Assert.AreEqual("Golarion", stored.Name);
        Assert.AreEqual("Pathfinder 2E Westmarch", stored.Description);
        Assert.AreEqual(GmRoleSnowflake, stored.GMRoleId);
        Assert.AreEqual(AdminChannelSnowflake, stored.AdminChannelId);
    }

    [TestMethod]
    public async Task Settings_StoredAsJsonText_DeserializesBack()
    {
        await using (var writeContext = MySqlContainerFixture.CreateContext())
        {
            var repo = new ServerRepository(writeContext);
            await repo.AddAsync(new Server
            {
                Id = 1,
                Name = "Absalom",
                GMRoleId = 10,
                AdminChannelId = 20,
                Settings = Settings(ServerMode.Westmarch, Color.Purple)
            });
            await repo.SaveAsync();
        }

        await using var readContext = MySqlContainerFixture.CreateContext();
        var stored = await new ServerRepository(readContext).GetByIdAsync(1, IncludeBehavior.NoInclude);

        var settings = JsonConvert.DeserializeObject<ServerSettings>(
            stored!.Settings, VoyagerJsonSettings.Default);
        Assert.IsNotNull(settings);
        Assert.AreEqual(ServerMode.Westmarch, settings!.Mode);
        Assert.AreEqual(Color.Purple, settings.GeneralColor);
    }

    [TestMethod]
    public async Task GetByNameAsync_TranslatesToSql_ReturnsMatch()
    {
        await using var context = MySqlContainerFixture.CreateContext();
        var repo = new ServerRepository(context);
        await repo.AddAsync(new Server { Id = 1, Name = "Alpha", GMRoleId = 10, AdminChannelId = 20, Settings = Settings() });
        await repo.AddAsync(new Server { Id = 2, Name = "Beta", GMRoleId = 11, AdminChannelId = 21, Settings = Settings() });
        await repo.SaveAsync();

        var result = await repo.GetByNameAsync("Beta", IncludeBehavior.NoInclude);

        Assert.IsNotNull(result);
        Assert.AreEqual(2UL, result!.Id);
    }

    [TestMethod]
    public async Task Update_PersistsAcrossContexts()
    {
        await using (var seed = MySqlContainerFixture.CreateContext())
        {
            var repo = new ServerRepository(seed);
            await repo.AddAsync(new Server { Id = 1, Name = "Before", GMRoleId = 10, AdminChannelId = 20, Settings = Settings() });
            await repo.SaveAsync();
        }

        await using (var edit = MySqlContainerFixture.CreateContext())
        {
            var repo = new ServerRepository(edit);
            var server = await repo.GetByIdAsync(1, IncludeBehavior.NoInclude);
            server!.Name = "After";
            server.Description = "Added description";
            repo.Update(server);
            await repo.SaveAsync();
        }

        await using var verify = MySqlContainerFixture.CreateContext();
        var reloaded = await new ServerRepository(verify).GetByIdAsync(1, IncludeBehavior.NoInclude);
        Assert.AreEqual("After", reloaded!.Name);
        Assert.AreEqual("Added description", reloaded.Description);
    }

    [TestMethod]
    public async Task Delete_PersistsAcrossContexts()
    {
        await using (var seed = MySqlContainerFixture.CreateContext())
        {
            var repo = new ServerRepository(seed);
            await repo.AddAsync(new Server { Id = 1, Name = "Doomed", GMRoleId = 10, AdminChannelId = 20, Settings = Settings() });
            await repo.SaveAsync();
        }

        await using (var del = MySqlContainerFixture.CreateContext())
        {
            var repo = new ServerRepository(del);
            var server = await repo.GetByIdAsync(1, IncludeBehavior.NoInclude);
            repo.Delete(server!);
            await repo.SaveAsync();
        }

        await using var verify = MySqlContainerFixture.CreateContext();
        var result = await new ServerRepository(verify).GetByIdAsync(1, IncludeBehavior.NoInclude);
        Assert.IsNull(result);
    }
}
