using Repository.Context;
using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories;
using VoyagerTests.TestHelpers;

namespace VoyagerTests.Repositories;

/// <summary>
/// Tests for <see cref="UserRepository"/> and the inherited generic
/// <c>BaseRepository</c> CRUD operations, run against the EF Core in-memory
/// provider.
/// </summary>
[TestClass]
public class UserRepositoryTests
{
    private static User MakeUser(ulong id, string name) =>
        new() { Id = id, Name = name, Settings = "{}" };

    private static async Task<VoyagerDbContext> SeedAsync(params User[] users)
    {
        var context = InMemoryDb.CreateContext();
        context.Users.AddRange(users);
        await context.SaveChangesAsync();
        return context;
    }

    // ---- GetByIdAsync (BaseRepository) ----------------------------------

    [TestMethod]
    public async Task GetByIdAsync_ReturnsMatchingUser()
    {
        await using var context = await SeedAsync(MakeUser(1, "Ada"), MakeUser(2, "Grace"));
        var repo = new UserRepository(context);

        var result = await repo.GetByIdAsync(2, IncludeBehavior.NoInclude);

        Assert.IsNotNull(result);
        Assert.AreEqual("Grace", result!.Name);
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        await using var context = await SeedAsync(MakeUser(1, "Ada"));
        var repo = new UserRepository(context);

        var result = await repo.GetByIdAsync(999, IncludeBehavior.NoInclude);

        Assert.IsNull(result);
    }

    // ---- GetByNameAsync (UserRepository) --------------------------------

    [TestMethod]
    public async Task GetByNameAsync_ReturnsMatchingUser()
    {
        await using var context = await SeedAsync(MakeUser(1, "Ada"), MakeUser(2, "Grace"));
        var repo = new UserRepository(context);

        var result = await repo.GetByNameAsync("Ada", IncludeBehavior.NoInclude);

        Assert.IsNotNull(result);
        Assert.AreEqual(1UL, result!.Id);
    }

    [TestMethod]
    public async Task GetByNameAsync_WhenMissing_ReturnsNull()
    {
        await using var context = await SeedAsync(MakeUser(1, "Ada"));
        var repo = new UserRepository(context);

        var result = await repo.GetByNameAsync("nobody", IncludeBehavior.NoInclude);

        Assert.IsNull(result);
    }

    // ---- GetAllAsync ----------------------------------------------------

    [TestMethod]
    public async Task GetAllAsync_ReturnsEveryUser()
    {
        await using var context = await SeedAsync(MakeUser(1, "A"), MakeUser(2, "B"), MakeUser(3, "C"));
        var repo = new UserRepository(context);

        var result = (await repo.GetAllAsync(IncludeBehavior.NoInclude)).ToList();

        Assert.HasCount(3, result);
    }

    // ---- AddAsync + SaveAsync -------------------------------------------

    [TestMethod]
    public async Task AddAsync_ThenSave_PersistsUser()
    {
        await using var context = InMemoryDb.CreateContext();
        var repo = new UserRepository(context);

        await repo.AddAsync(MakeUser(1, "Fresh"));
        await repo.SaveAsync();

        var stored = await repo.GetByIdAsync(1, IncludeBehavior.NoInclude);
        Assert.IsNotNull(stored);
        Assert.AreEqual("Fresh", stored!.Name);
    }

    [TestMethod]
    public async Task AddAsync_WithoutSave_DoesNotPersist()
    {
        // Two contexts share the same in-memory store so we can observe what
        // was actually committed vs. only tracked.
        var dbName = Guid.NewGuid().ToString();

        await using (var writeContext = InMemoryDb.CreateContext(dbName))
        {
            var repo = new UserRepository(writeContext);
            await repo.AddAsync(MakeUser(1, "Unsaved"));
            // intentionally no SaveAsync
        }

        await using var readContext = InMemoryDb.CreateContext(dbName);
        var stored = await new UserRepository(readContext)
            .GetByIdAsync(1, IncludeBehavior.NoInclude);
        Assert.IsNull(stored);
    }

    // ---- Update + SaveAsync ---------------------------------------------

    [TestMethod]
    public async Task Update_ThenSave_ModifiesUser()
    {
        await using var context = await SeedAsync(MakeUser(1, "Before"));
        var repo = new UserRepository(context);

        var user = await repo.GetByIdAsync(1, IncludeBehavior.NoInclude);
        user!.Name = "After";
        repo.Update(user);
        await repo.SaveAsync();

        var reloaded = await repo.GetByIdAsync(1, IncludeBehavior.NoInclude);
        Assert.AreEqual("After", reloaded!.Name);
    }

    // ---- Delete + SaveAsync ---------------------------------------------

    [TestMethod]
    public async Task Delete_ThenSave_RemovesUser()
    {
        await using var context = await SeedAsync(MakeUser(1, "Doomed"));
        var repo = new UserRepository(context);

        var user = await repo.GetByIdAsync(1, IncludeBehavior.NoInclude);
        repo.Delete(user!);
        await repo.SaveAsync();

        var result = await repo.GetByIdAsync(1, IncludeBehavior.NoInclude);
        Assert.IsNull(result);
    }
}
