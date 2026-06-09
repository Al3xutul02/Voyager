using Microsoft.EntityFrameworkCore;
using Repository.Context;

namespace VoyagerTests.TestHelpers;

/// <summary>
/// Creates isolated <see cref="VoyagerDbContext"/> instances backed by the
/// EF Core in-memory provider. Each call uses a unique database name so test
/// methods never share state.
///
/// <para>
/// NOTE: the in-memory provider does not enforce relational constraints
/// (max length, column types, FK rules). It's sufficient for exercising
/// repository query/CRUD logic, but it is not a substitute for testing
/// against real MySQL when validating schema behavior.
/// </para>
/// </summary>
public static class InMemoryDb
{
    /// <summary>
    /// Creates a context over a brand-new, isolated in-memory database.
    /// </summary>
    public static VoyagerDbContext CreateContext() => CreateContext(Guid.NewGuid().ToString());

    /// <summary>
    /// Creates a context over the named in-memory database. Pass the same
    /// name to two contexts to share committed state between them (useful for
    /// testing that <c>SaveChanges</c> actually persists).
    /// </summary>
    public static VoyagerDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<VoyagerDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new VoyagerDbContext(options);
    }
}
