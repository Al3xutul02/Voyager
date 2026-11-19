using Repository.Context;
using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories;
using VoyagerTests.TestHelpers;

namespace VoyagerTests.Repositories;

/// <summary>
/// Tests for <see cref="ServerRepository"/> and the inherited generic
/// <c>BaseRepository</c> CRUD operations, run against the EF Core in-memory
/// provider.
/// </summary>
[TestClass]
public class ServerRepositoryTests
{
    private static Server MakeServer(
        ulong id,
        string name,
        string? description = null,
        ulong gmRoleId = 10,
        ulong adminChannelId = 20) =>
        new()
        {
            Id = id,
            Name = name,
            Description = description,
            GMRoleId = gmRoleId,
            AdminChannelId = adminChannelId,
            Settings = "{}"
        };

    private static async Task<VoyagerDbContext> SeedAsync(params Server[] servers)
    {
        var context = InMemoryDb.CreateContext();
        context.Servers.AddRange(servers);
        await context.SaveChangesAsync();
        return context;
    }

    // ---- GetByIdAsync (BaseRepository) ----------------------------------

    [TestMethod]
    public async Task GetByIdAsync_ReturnsMatchingServer()
    {
        await using var context = await SeedAsync(MakeServer(100, "Alpha"), MakeServer(200, "Beta"));
        var repo = new ServerRepository(context);

        var result = await repo.GetByIdAsync(200, IncludeBehavior.NoInclude);

        Assert.IsNotNull(result);
        Assert.AreEqual("Beta", result!.Name);
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        await using var context = await SeedAsync(MakeServer(100, "Alpha"));
        var repo = new ServerRepository(context);

        var result = await repo.GetByIdAsync(999, IncludeBehavior.NoInclude);

        Assert.IsNull(result);
    }

    // ---- GetByNameAsync (ServerRepository) ------------------------------

    [TestMethod]
    public async Task GetByNameAsync_ReturnsMatchingServer()
    {
        await using var context = await SeedAsync(MakeServer(100, "Alpha"), MakeServer(200, "Beta"));
        var repo = new ServerRepository(context);

        var result = await repo.GetByNameAsync("Alpha", IncludeBehavior.NoInclude);

        Assert.IsNotNull(result);
        Assert.AreEqual(100UL, result!.Id);
    }

    [TestMethod]
    public async Task GetByNameAsync_WhenMissing_ReturnsNull()
    {
        await using var context = await SeedAsync(MakeServer(100, "Alpha"));
        var repo = new ServerRepository(context);

        var result = await repo.GetByNameAsync("UnknownGuild", IncludeBehavior.NoInclude);

        Assert.IsNull(result);
    }

    // ---- GetAllAsync ----------------------------------------------------

    [TestMethod]
    public async Task GetAllAsync_ReturnsEveryServer()
    {
        await using var context = await SeedAsync(
            MakeServer(1, "S1"),
            MakeServer(2, "S2"),
            MakeServer(3, "S3"));
        var repo = new ServerRepository(context);

        var result = (await repo.GetAllAsync(IncludeBehavior.NoInclude)).ToList();

        Assert.HasCount(3, result);
    }

    // ---- AddAsync + SaveAsync -------------------------------------------

    [TestMethod]
    public async Task AddAsync_ThenSave_PersistsServer()
    {
        await using var context = InMemoryDb.CreateContext();
        var repo = new ServerRepository(context);

        await repo.AddAsync(MakeServer(1, "NewGuild", "Pathfinder 2E Hub", 111, 222));
        await repo.SaveAsync();

        var stored = await repo.GetByIdAsync(1, IncludeBehavior.NoInclude);
        Assert.IsNotNull(stored);
        Assert.AreEqual("NewGuild", stored!.Name);
        Assert.AreEqual("Pathfinder 2E Hub", stored.Description);
        Assert.AreEqual(111UL, stored.GMRoleId);
        Assert.AreEqual(222UL, stored.AdminChannelId);
    }

    [TestMethod]
    public async Task AddAsync_WithoutSave_DoesNotPersist()
    {
        var dbName = Guid.NewGuid().ToString();

        await using (var writeContext = InMemoryDb.CreateContext(dbName))
        {
            var repo = new ServerRepository(writeContext);
            await repo.AddAsync(MakeServer(1, "UnsavedGuild"));
        }

        await using var readContext = InMemoryDb.CreateContext(dbName);
        var stored = await new ServerRepository(readContext)
            .GetByIdAsync(1, IncludeBehavior.NoInclude);
        Assert.IsNull(stored);
    }

    // ---- Update + SaveAsync ---------------------------------------------

    [TestMethod]
    public async Task Update_ThenSave_ModifiesServer()
    {
        await using var context = await SeedAsync(MakeServer(1, "Before"));
        var repo = new ServerRepository(context);

        var server = await repo.GetByIdAsync(1, IncludeBehavior.NoInclude);
        server!.Name = "After";
        server.Description = "Updated description";
        repo.Update(server);
        await repo.SaveAsync();

        var reloaded = await repo.GetByIdAsync(1, IncludeBehavior.NoInclude);
        Assert.AreEqual("After", reloaded!.Name);
        Assert.AreEqual("Updated description", reloaded.Description);
    }

    [TestMethod]
    public async Task Update_WithNewEntityInstanceWhenAlreadyTracked_DetachesOldAndModifiesServer()
    {
        await using var context = await SeedAsync(MakeServer(1, "Before"));
        var repo = new ServerRepository(context);

        _ = await repo.GetByIdAsync(1, IncludeBehavior.NoInclude);
        var mappedUpdate = MakeServer(1, "UpdatedGuild", "New desc", 555, 666);

        repo.Update(mappedUpdate);
        await repo.SaveAsync();

        var reloaded = await repo.GetByIdAsync(1, IncludeBehavior.NoInclude);
        Assert.AreEqual("UpdatedGuild", reloaded!.Name);
        Assert.AreEqual("New desc", reloaded.Description);
        Assert.AreEqual(555UL, reloaded.GMRoleId);
        Assert.AreEqual(666UL, reloaded.AdminChannelId);
    }

    // ---- Delete + SaveAsync ---------------------------------------------

    [TestMethod]
    public async Task Delete_ThenSave_RemovesServer()
    {
        await using var context = await SeedAsync(MakeServer(1, "DoomedGuild"));
        var repo = new ServerRepository(context);

        var server = await repo.GetByIdAsync(1, IncludeBehavior.NoInclude);
        repo.Delete(server!);
        await repo.SaveAsync();

        var result = await repo.GetByIdAsync(1, IncludeBehavior.NoInclude);
        Assert.IsNull(result);
    }
}
