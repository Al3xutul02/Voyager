using Microsoft.EntityFrameworkCore;
using Repository.Context;
using Testcontainers.MySql;

namespace VoyagerTests.Integration;

/// <summary>
/// Owns a throwaway MySQL container shared by the integration tests.
///
/// <para>
/// Unlike an <c>[AssemblyInitialize]</c> hook, this is started on demand via
/// <see cref="StartAsync"/> from an integration test class's
/// <c>[ClassInitialize]</c>. That means a unit-tests-only run never starts
/// Docker — the container only spins up when an integration test actually
/// executes.
/// </para>
///
/// <para>
/// Requires Docker to be running. The first run pulls the MySQL image, which
/// can take a couple of minutes; subsequent runs reuse the cached image.
/// </para>
/// </summary>
public static class MySqlContainerFixture
{
    private static MySqlContainer? _container;

    /// <summary>Connection string to the running container's database.</summary>
    public static string ConnectionString { get; private set; } = null!;

    /// <summary>Detected server version, cached so each context build doesn't re-probe.</summary>
    public static ServerVersion ServerVersion { get; private set; } = null!;

    /// <summary>
    /// Starts the container (if not already running) and creates the schema.
    /// Safe to call once per integration test class.
    /// </summary>
    public static async Task StartAsync()
    {
        if (_container is not null)
        {
            return;
        }

        _container = new MySqlBuilder("mysql:8.4")
            .WithDatabase("voyager_test")
            .Build();

        await _container.StartAsync();

        ConnectionString = _container.GetConnectionString();
        ServerVersion = ServerVersion.AutoDetect(ConnectionString);

        // Build the schema once from the model (no migrations in this project).
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    /// <summary>Stops and disposes the container.</summary>
    public static async Task StopAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
            _container = null;
        }
    }

    /// <summary>
    /// Creates a fresh <see cref="VoyagerDbContext"/> pointed at the container.
    /// Each test should use its own context (often more than one) to prove data
    /// is actually round-tripping through MySQL, not living in a change tracker.
    /// </summary>
    public static VoyagerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<VoyagerDbContext>()
            .UseMySql(ConnectionString, ServerVersion)
            .Options;

        return new VoyagerDbContext(options);
    }

    /// <summary>
    /// Removes all rows from the users table so each test starts clean. Faster
    /// than dropping/recreating the schema between tests.
    /// </summary>
    public static async Task ResetAsync()
    {
        await using var context = CreateContext();
        await context.Users.ExecuteDeleteAsync();
    }
}
