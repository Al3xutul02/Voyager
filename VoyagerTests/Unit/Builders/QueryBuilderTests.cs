using Microsoft.EntityFrameworkCore;
using Repository.Builders;
using Repository.Enums.Behaviors;
using Repository.Models;
using VoyagerTests.TestHelpers;

namespace VoyagerTests.Builders;

/// <summary>
/// Tests for the generic <see cref="QueryBuilder{T}"/> used by the
/// repositories to compose include behavior.
/// </summary>
[TestClass]
public class QueryBuilderTests
{
    private static async Task<Repository.Context.VoyagerDbContext> SeedAsync()
    {
        var context = InMemoryDb.CreateContext();
        context.Users.AddRange(
            new User { Id = 1, Name = "A", Settings = "{}" },
            new User { Id = 2, Name = "B", Settings = "{}" });
        await context.SaveChangesAsync();
        return context;
    }

    [TestMethod]
    public void AddIncludes_ReturnsSameBuilderInstance()
    {
        using var context = InMemoryDb.CreateContext();
        var builder = new QueryBuilder<User>(context.Users);

        var returned = builder.AddIncludes(null);

        Assert.AreSame(builder, returned);
    }

    [TestMethod]
    public void AddBehavior_ReturnsSameBuilderInstance()
    {
        using var context = InMemoryDb.CreateContext();
        var builder = new QueryBuilder<User>(context.Users);

        var returned = builder.AddBehavior(IncludeBehavior.NoInclude);

        Assert.AreSame(builder, returned);
    }

    [TestMethod]
    public async Task Build_WithNoInclude_ReturnsAllRows()
    {
        await using var context = await SeedAsync();

        var query = new QueryBuilder<User>(context.Users)
            .AddBehavior(IncludeBehavior.NoInclude)
            .Build();

        Assert.AreEqual(2, await query.CountAsync());
    }

    [TestMethod]
    public async Task Build_WithAllIncludes_ReturnsAllRows()
    {
        // User has no navigation properties, so AllIncludes is a no-op on the
        // result set — but it must still execute without throwing.
        await using var context = await SeedAsync();

        var query = new QueryBuilder<User>(context.Users)
            .AddBehavior(IncludeBehavior.AllIncludes)
            .Build();

        Assert.AreEqual(2, await query.CountAsync());
    }

    [TestMethod]
    public async Task Build_WithGivenIncludes_AppliesProvidedFunction()
    {
        await using var context = await SeedAsync();

        var query = new QueryBuilder<User>(context.Users)
            .AddIncludes(q => q.Where(u => u.Id == 1))
            .AddBehavior(IncludeBehavior.GivenIncludes)
            .Build();

        var results = await query.ToListAsync();
        Assert.HasCount(1, results);
        Assert.AreEqual(1UL, results[0].Id);
    }
}
