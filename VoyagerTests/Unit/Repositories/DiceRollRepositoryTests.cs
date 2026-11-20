using Repository.Context;
using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories;
using VoyagerTests.TestHelpers;

namespace VoyagerTests.Repositories;

/// <summary>
/// Tests for <see cref="DiceRollRepository"/> and the inherited generic
/// <c>BaseRepository</c> CRUD operations, run against the EF Core in-memory
/// provider.
/// </summary>
[TestClass]
public class DiceRollRepositoryTests
{
    private static DiceRoll MakeDiceRoll(
        ulong id,
        ulong userId,
        string name,
        string formula = "1d20+5") =>
        new()
        {
            Id = id,
            UserId = userId,
            Name = name,
            Formula = formula
        };

    private static async Task<VoyagerDbContext> SeedAsync(params DiceRoll[] diceRolls)
    {
        var context = InMemoryDb.CreateContext();
        context.DiceRolls.AddRange(diceRolls);
        await context.SaveChangesAsync();
        return context;
    }

    // ---- GetByIdAsync (BaseRepository) ----------------------------------

    [TestMethod]
    public async Task GetByIdAsync_ReturnsMatchingDiceRoll()
    {
        await using var context = await SeedAsync(
            MakeDiceRoll(1, 10, "Attack", "1d20+7"),
            MakeDiceRoll(2, 10, "Fireball", "6d6[fire]"));
        var repo = new DiceRollRepository(context);

        var result = await repo.GetByIdAsync(2, IncludeBehavior.NoInclude);

        Assert.IsNotNull(result);
        Assert.AreEqual("Fireball", result!.Name);
        Assert.AreEqual("6d6[fire]", result.Formula);
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        await using var context = await SeedAsync(MakeDiceRoll(1, 10, "Attack"));
        var repo = new DiceRollRepository(context);

        var result = await repo.GetByIdAsync(999, IncludeBehavior.NoInclude);

        Assert.IsNull(result);
    }

    // ---- GetByUserIdAsync -----------------------------------------------

    [TestMethod]
    public async Task GetByUserIdAsync_ReturnsOnlyRollsOwnedByUser()
    {
        await using var context = await SeedAsync(
            MakeDiceRoll(1, 10, "RollA", "1d20"),
            MakeDiceRoll(2, 10, "RollB", "2d6"),
            MakeDiceRoll(3, 99, "OtherUserRoll", "1d8"));
        var repo = new DiceRollRepository(context);

        var results = (await repo.GetByUserIdAsync(10, IncludeBehavior.NoInclude)).ToList();

        Assert.HasCount(2, results);
        CollectionAssert.AreEquivalent(new[] { "RollA", "RollB" }, results.Select(r => r.Name).ToList());
    }

    [TestMethod]
    public async Task GetByUserIdAsync_WhenUserHasNoRolls_ReturnsEmpty()
    {
        await using var context = await SeedAsync(MakeDiceRoll(1, 10, "RollA", "1d20"));
        var repo = new DiceRollRepository(context);

        var results = (await repo.GetByUserIdAsync(777, IncludeBehavior.NoInclude)).ToList();

        Assert.IsEmpty(results);
    }

    // ---- GetByUserAndNameAsync ------------------------------------------

    [TestMethod]
    public async Task GetByUserAndNameAsync_ReturnsMatchingRollForUser()
    {
        await using var context = await SeedAsync(
            MakeDiceRoll(1, 10, "Strike", "1d20+9"),
            MakeDiceRoll(2, 20, "Strike", "1d20+3"));
        var repo = new DiceRollRepository(context);

        var result = await repo.GetByUserAndNameAsync(20, "Strike", IncludeBehavior.NoInclude);

        Assert.IsNotNull(result);
        Assert.AreEqual(2UL, result!.Id);
        Assert.AreEqual("1d20+3", result.Formula);
    }

    [TestMethod]
    public async Task GetByUserAndNameAsync_WhenMissing_ReturnsNull()
    {
        await using var context = await SeedAsync(MakeDiceRoll(1, 10, "Strike", "1d20+9"));
        var repo = new DiceRollRepository(context);

        var result = await repo.GetByUserAndNameAsync(10, "NonExistent", IncludeBehavior.NoInclude);

        Assert.IsNull(result);
    }

    // ---- GetByUserAndFormulaAsync ---------------------------------------

    [TestMethod]
    public async Task GetByUserAndFormulaAsync_ReturnsMatchingRollForUser()
    {
        await using var context = await SeedAsync(
            MakeDiceRoll(1, 10, "StatsRoll", "4d6h2+4"),
            MakeDiceRoll(2, 20, "OtherStats", "4d6h2+4"));
        var repo = new DiceRollRepository(context);

        var result = await repo.GetByUserAndFormulaAsync(10, "4d6h2+4", IncludeBehavior.NoInclude);

        Assert.IsNotNull(result);
        Assert.AreEqual(1UL, result!.Id);
        Assert.AreEqual("StatsRoll", result.Name);
    }

    [TestMethod]
    public async Task GetByUserAndFormulaAsync_WhenMissing_ReturnsNull()
    {
        await using var context = await SeedAsync(MakeDiceRoll(1, 10, "StatsRoll", "4d6h2+4"));
        var repo = new DiceRollRepository(context);

        var result = await repo.GetByUserAndFormulaAsync(10, "2d8h1", IncludeBehavior.NoInclude);

        Assert.IsNull(result);
    }

    // ---- AddAsync / Update / Delete + SaveAsync -------------------------

    [TestMethod]
    public async Task AddUpdateDelete_RoundTripsDiceRoll()
    {
        await using var context = InMemoryDb.CreateContext();
        var repo = new DiceRollRepository(context);

        var created = MakeDiceRoll(1, 42, "Heal", "2d8+16");
        await repo.AddAsync(created);
        await repo.SaveAsync();

        var fetched = await repo.GetByIdAsync(1, IncludeBehavior.NoInclude);
        Assert.IsNotNull(fetched);
        Assert.AreEqual("Heal", fetched!.Name);

        fetched.Formula = "3d8+24";
        repo.Update(fetched);
        await repo.SaveAsync();

        var updated = await repo.GetByIdAsync(1, IncludeBehavior.NoInclude);
        Assert.AreEqual("3d8+24", updated!.Formula);

        repo.Delete(updated);
        await repo.SaveAsync();

        var afterDelete = await repo.GetByIdAsync(1, IncludeBehavior.NoInclude);
        Assert.IsNull(afterDelete);
    }
}
