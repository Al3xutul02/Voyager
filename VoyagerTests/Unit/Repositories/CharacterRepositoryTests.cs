using Repository.Context;
using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories;
using VoyagerTests.TestHelpers;

namespace VoyagerTests.Repositories;

/// <summary>
/// Tests for <see cref="CharacterRepository"/> and inherited generic
/// <c>BaseRepository</c> CRUD operations, run against the EF Core in-memory provider.
/// </summary>
[TestClass]
public class CharacterRepositoryTests
{
    private static Character MakeCharacter(
        ulong id,
        ulong userId,
        string name,
        string? details = null,
        DateTime? deletedAt = null) =>
        new()
        {
            Id = id,
            UserId = userId,
            Name = name,
            Description = "A brave adventurer",
            Color = 1,
            TokenImagePath = "Storage/Characters/10/1/token.png",
            ActorImagePath = "Storage/Characters/10/1/actor.png",
            CurrentGame = null,
            Gold = 10.5m,
            PathbuilderId = "123456",
            Details = details ?? "{\"Ancestry\":\"Human\",\"Class\":\"Rogue\",\"Level\":\"3\",\"Age\":\"25\",\"Gender\":\"Male\"}",
            DeletedAt = deletedAt
        };

    private static async Task<VoyagerDbContext> SeedAsync(params Character[] characters)
    {
        var context = InMemoryDb.CreateContext();
        context.Characters.AddRange(characters);
        await context.SaveChangesAsync();
        return context;
    }

    // ---- GetByIdAsync (BaseRepository) ----------------------------------

    [TestMethod]
    public async Task GetByIdAsync_ReturnsMatchingCharacter()
    {
        await using var context = await SeedAsync(
            MakeCharacter(1, 10, "Sunaki"),
            MakeCharacter(2, 10, "Valeros"));
        var repo = new CharacterRepository(context);

        var result = await repo.GetByIdAsync(2, IncludeBehavior.NoInclude);

        Assert.IsNotNull(result);
        Assert.AreEqual("Valeros", result!.Name);
        Assert.AreEqual(10UL, result.UserId);
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        await using var context = await SeedAsync(MakeCharacter(1, 10, "Sunaki"));
        var repo = new CharacterRepository(context);

        var result = await repo.GetByIdAsync(999, IncludeBehavior.NoInclude);

        Assert.IsNull(result);
    }

    // ---- GetByUserIdAsync -----------------------------------------------

    [TestMethod]
    public async Task GetByUserIdAsync_ReturnsOnlyCharactersOwnedByUser()
    {
        await using var context = await SeedAsync(
            MakeCharacter(1, 10, "Sunaki"),
            MakeCharacter(2, 10, "Valeros"),
            MakeCharacter(3, 99, "OtherPlayerChar"));
        var repo = new CharacterRepository(context);

        var results = (await repo.GetByUserIdAsync(10, IncludeBehavior.NoInclude)).ToList();

        Assert.HasCount(2, results);
        CollectionAssert.AreEquivalent(new[] { "Sunaki", "Valeros" }, results.Select(c => c.Name).ToList());
    }

    [TestMethod]
    public async Task GetByUserIdAsync_WhenUserHasNoCharacters_ReturnsEmpty()
    {
        await using var context = await SeedAsync(MakeCharacter(1, 10, "Sunaki"));
        var repo = new CharacterRepository(context);

        var results = (await repo.GetByUserIdAsync(777, IncludeBehavior.NoInclude)).ToList();

        Assert.IsEmpty(results);
    }

    // ---- GetByUserAndNameAsync ------------------------------------------

    [TestMethod]
    public async Task GetByUserAndNameAsync_ReturnsMatchingCharacterForUser()
    {
        await using var context = await SeedAsync(
            MakeCharacter(1, 10, "Sunaki"),
            MakeCharacter(2, 10, "Valeros"),
            MakeCharacter(3, 99, "Sunaki")); // Same name but different user
        var repo = new CharacterRepository(context);

        var result = await repo.GetByUserAndNameAsync(10, "Sunaki", IncludeBehavior.NoInclude);

        Assert.IsNotNull(result);
        Assert.AreEqual(1UL, result!.Id);
        Assert.AreEqual(10UL, result.UserId);
        Assert.AreEqual("Sunaki", result.Name);
    }

    [TestMethod]
    public async Task GetByUserAndNameAsync_WhenNameNotFound_ReturnsNull()
    {
        await using var context = await SeedAsync(MakeCharacter(1, 10, "Sunaki"));
        var repo = new CharacterRepository(context);

        var result = await repo.GetByUserAndNameAsync(10, "NonExistent", IncludeBehavior.NoInclude);

        Assert.IsNull(result);
    }

    // ---- GetCountByUserIdAsync ------------------------------------------

    [TestMethod]
    public async Task GetCountByUserIdAsync_CountsOnlyActiveCharactersForUser()
    {
        await using var context = await SeedAsync(
            MakeCharacter(1, 10, "Active1"),
            MakeCharacter(2, 10, "Active2"),
            MakeCharacter(3, 10, "DeletedChar", deletedAt: DateTime.UtcNow),
            MakeCharacter(4, 99, "OtherUserChar"));
        var repo = new CharacterRepository(context);

        var count = await repo.GetCountByUserIdAsync(10);

        Assert.AreEqual(2, count);
    }

    // ---- AddAsync / SaveAsync -------------------------------------------

    [TestMethod]
    public async Task AddAsync_PersistsNewCharacter()
    {
        var dbName = Guid.NewGuid().ToString();
        await using (var context = InMemoryDb.CreateContext(dbName))
        {
            var repo = new CharacterRepository(context);
            await repo.AddAsync(MakeCharacter(1, 10, "Kyra"));
            await repo.SaveAsync();
        }

        await using (var verifyContext = InMemoryDb.CreateContext(dbName))
        {
            var repo = new CharacterRepository(verifyContext);
            var result = await repo.GetByIdAsync(1, IncludeBehavior.NoInclude);

            Assert.IsNotNull(result);
            Assert.AreEqual("Kyra", result!.Name);
        }
    }

    // ---- Update / SaveAsync ---------------------------------------------

    [TestMethod]
    public async Task Update_PersistsModifiedCharacter()
    {
        var dbName = Guid.NewGuid().ToString();
        await using (var context = InMemoryDb.CreateContext(dbName))
        {
            var repo = new CharacterRepository(context);
            await repo.AddAsync(MakeCharacter(1, 10, "Farsight"));
            await repo.SaveAsync();
        }

        await using (var updateContext = InMemoryDb.CreateContext(dbName))
        {
            var repo = new CharacterRepository(updateContext);
            var existing = await repo.GetByIdAsync(1, IncludeBehavior.NoInclude);
            Assert.IsNotNull(existing);

            existing!.Gold = 50.0m;
            existing.CurrentGame = "Test Campaign";
            repo.Update(existing);
            await repo.SaveAsync();
        }

        await using (var verifyContext = InMemoryDb.CreateContext(dbName))
        {
            var repo = new CharacterRepository(verifyContext);
            var result = await repo.GetByIdAsync(1, IncludeBehavior.NoInclude);

            Assert.IsNotNull(result);
            Assert.AreEqual(50.0m, result!.Gold);
            Assert.AreEqual("Test Campaign", result.CurrentGame);
        }
    }

    // ---- Delete / SaveAsync ---------------------------------------------

    [TestMethod]
    public async Task Delete_RemovesCharacterFromDatabase()
    {
        var dbName = Guid.NewGuid().ToString();
        await using (var context = InMemoryDb.CreateContext(dbName))
        {
            var repo = new CharacterRepository(context);
            await repo.AddAsync(MakeCharacter(1, 10, "DoomedChar"));
            await repo.SaveAsync();
        }

        await using (var deleteContext = InMemoryDb.CreateContext(dbName))
        {
            var repo = new CharacterRepository(deleteContext);
            var existing = await repo.GetByIdAsync(1, IncludeBehavior.NoInclude);
            Assert.IsNotNull(existing);

            repo.Delete(existing!);
            await repo.SaveAsync();
        }

        await using (var verifyContext = InMemoryDb.CreateContext(dbName))
        {
            var repo = new CharacterRepository(verifyContext);
            var result = await repo.GetByIdAsync(1, IncludeBehavior.NoInclude);

            Assert.IsNull(result);
        }
    }
}
