using BusinessLogic.Enums.Types;
using BusinessLogic.Json;
using BusinessLogic.Json.Models;
using Newtonsoft.Json;
using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories;

namespace VoyagerTests.Integration;

/// <summary>
/// Repository tests that run against a real MySQL instance (via Testcontainers).
/// These validate behavior the EF Core in-memory provider can't: the actual
/// schema from <c>OnModelCreating</c>, Pomelo's LINQ->MySQL translation, ulong
/// (BIGINT UNSIGNED) key handling, and the <c>text</c> Settings column.
///
/// <para>
/// Marked <see cref="DoNotParallelizeAttribute"/> because every test shares
/// the one container and resets the same <c>users</c> table between runs —
/// they must execute sequentially even though the rest of the assembly runs
/// in parallel (see MSTestSettings.cs).
/// </para>
/// </summary>
[TestClass]
[DoNotParallelize]
[TestCategory("Integration")]
public class UserRepositoryIntegrationTests
{
    // A realistic Discord snowflake — larger than int.MaxValue, exercising the
    // ulong -> BIGINT UNSIGNED mapping that InMemory never validates.
    private const ulong Snowflake = 1320120043694194732UL;

    private static string Settings(Color color) =>
        JsonConvert.SerializeObject(new UserSettings(color), VoyagerJsonSettings.Default);

    [ClassInitialize]
    public static async Task StartContainer(TestContext _) => await MySqlContainerFixture.StartAsync();

    [ClassCleanup]
    public static async Task StopContainer() => await MySqlContainerFixture.StopAsync();

    [TestInitialize]
    public async Task ResetDatabase() => await MySqlContainerFixture.ResetAsync();

    [TestMethod]
    public async Task AddAsync_RealSnowflakeId_RoundTripsThroughMySql()
    {
        // Arrange + Act: write with one context...
        await using (var writeContext = MySqlContainerFixture.CreateContext())
        {
            var repo = new UserRepository(writeContext);
            await repo.AddAsync(new User { Id = Snowflake, Name = "Ada", Settings = Settings(Color.Red) });
            await repo.SaveAsync();
        }

        // ...read back with a *fresh* context to prove it persisted to MySQL.
        await using var readContext = MySqlContainerFixture.CreateContext();
        var stored = await new UserRepository(readContext)
            .GetByIdAsync(Snowflake, IncludeBehavior.NoInclude);

        Assert.IsNotNull(stored);
        Assert.AreEqual(Snowflake, stored!.Id);
        Assert.AreEqual("Ada", stored.Name);
    }

    [TestMethod]
    public async Task Settings_StoredAsJsonText_DeserializesBack()
    {
        await using (var writeContext = MySqlContainerFixture.CreateContext())
        {
            var repo = new UserRepository(writeContext);
            await repo.AddAsync(new User { Id = 1, Name = "Grace", Settings = Settings(Color.Magenta) });
            await repo.SaveAsync();
        }

        await using var readContext = MySqlContainerFixture.CreateContext();
        var stored = await new UserRepository(readContext).GetByIdAsync(1, IncludeBehavior.NoInclude);

        var settings = JsonConvert.DeserializeObject<UserSettings>(
            stored!.Settings, VoyagerJsonSettings.Default);
        Assert.IsNotNull(settings);
        Assert.AreEqual(Color.Magenta, settings!.Color);
    }

    [TestMethod]
    public async Task GetByIdAsync_TranslatesEfPropertyToSql()
    {
        // BaseRepository filters with EF.Property<ulong>(e, "Id"); this only
        // proves it translates to SQL when run against a real provider.
        await using var context = MySqlContainerFixture.CreateContext();
        var repo = new UserRepository(context);
        await repo.AddAsync(new User { Id = 50, Name = "Linus", Settings = Settings(Color.Green) });
        await repo.SaveAsync();

        var found = await repo.GetByIdAsync(50, IncludeBehavior.NoInclude);
        var missing = await repo.GetByIdAsync(51, IncludeBehavior.NoInclude);

        Assert.IsNotNull(found);
        Assert.IsNull(missing);
    }

    [TestMethod]
    public async Task GetByNameAsync_TranslatesToSql_ReturnsMatch()
    {
        await using var context = MySqlContainerFixture.CreateContext();
        var repo = new UserRepository(context);
        await repo.AddAsync(new User { Id = 1, Name = "Ada", Settings = Settings(Color.Teal) });
        await repo.AddAsync(new User { Id = 2, Name = "Grace", Settings = Settings(Color.Teal) });
        await repo.SaveAsync();

        var result = await repo.GetByNameAsync("Grace", IncludeBehavior.NoInclude);

        Assert.IsNotNull(result);
        Assert.AreEqual(2UL, result!.Id);
    }

    [TestMethod]
    public async Task GetAllAsync_ReturnsEveryRow()
    {
        await using var context = MySqlContainerFixture.CreateContext();
        var repo = new UserRepository(context);
        await repo.AddAsync(new User { Id = 1, Name = "A", Settings = Settings(Color.Teal) });
        await repo.AddAsync(new User { Id = 2, Name = "B", Settings = Settings(Color.Teal) });
        await repo.AddAsync(new User { Id = 3, Name = "C", Settings = Settings(Color.Teal) });
        await repo.SaveAsync();

        var all = (await repo.GetAllAsync(IncludeBehavior.NoInclude)).ToList();

        Assert.HasCount(3, all);
    }

    [TestMethod]
    public async Task Update_PersistsAcrossContexts()
    {
        await using (var seed = MySqlContainerFixture.CreateContext())
        {
            var repo = new UserRepository(seed);
            await repo.AddAsync(new User { Id = 1, Name = "Before", Settings = Settings(Color.Teal) });
            await repo.SaveAsync();
        }

        await using (var edit = MySqlContainerFixture.CreateContext())
        {
            var repo = new UserRepository(edit);
            var user = await repo.GetByIdAsync(1, IncludeBehavior.NoInclude);
            user!.Name = "After";
            repo.Update(user);
            await repo.SaveAsync();
        }

        await using var verify = MySqlContainerFixture.CreateContext();
        var reloaded = await new UserRepository(verify).GetByIdAsync(1, IncludeBehavior.NoInclude);
        Assert.AreEqual("After", reloaded!.Name);
    }

    [TestMethod]
    public async Task Delete_PersistsAcrossContexts()
    {
        await using (var seed = MySqlContainerFixture.CreateContext())
        {
            var repo = new UserRepository(seed);
            await repo.AddAsync(new User { Id = 1, Name = "Doomed", Settings = Settings(Color.Teal) });
            await repo.SaveAsync();
        }

        await using (var del = MySqlContainerFixture.CreateContext())
        {
            var repo = new UserRepository(del);
            var user = await repo.GetByIdAsync(1, IncludeBehavior.NoInclude);
            repo.Delete(user!);
            await repo.SaveAsync();
        }

        await using var verify = MySqlContainerFixture.CreateContext();
        var result = await new UserRepository(verify).GetByIdAsync(1, IncludeBehavior.NoInclude);
        Assert.IsNull(result);
    }
}
