using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Repository.Context;

/// <summary>
/// Design-time factory used by the EF Core tools (<c>dotnet ef migrations</c>,
/// <c>dotnet ef database update</c>). EF instantiates the context through this
/// instead of booting the bot's <c>Program.cs</c> — which would otherwise try
/// to connect to Discord and run the whole host just to read the model.
///
/// <para>
/// <b>Generating</b> a migration never connects to a database, so the
/// placeholder connection string below is fine for <c>migrations add</c>.
/// <b>Applying</b> one (<c>database update</c>) does connect — set the
/// <c>VOYAGER_DB_CONNECTION</c> environment variable to your real connection
/// string first, e.g. (PowerShell):
/// <code>$env:VOYAGER_DB_CONNECTION = "server=localhost;port=3306;database=voyager;user=root;password=..."</code>
/// </para>
/// </summary>
public class VoyagerDbContextFactory : IDesignTimeDbContextFactory<VoyagerDbContext>
{
    public VoyagerDbContext CreateDbContext(string[] args)
    {
        // Real value comes from the environment so no connection string (and no
        // credentials) is ever committed. The placeholder is only ever used for
        // `migrations add`, which doesn't open a connection.
        var connectionString =
            Environment.GetEnvironmentVariable("VOYAGER_DB_CONNECTION")
            ?? "server=localhost;port=3306;database=voyager_design;user=root;password=";

        // Pin an explicit server version so the tools don't try to auto-detect
        // (which would require a live connection just to scaffold a migration).
        var serverVersion = new MySqlServerVersion(new Version(8, 4, 0));

        var options = new DbContextOptionsBuilder<VoyagerDbContext>()
            .UseMySql(connectionString, serverVersion)
            .Options;

        return new VoyagerDbContext(options);
    }
}
