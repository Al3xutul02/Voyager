namespace Repository.Models;

/// <summary>
/// Database model for user information.
/// </summary>
public class User
{
    /// <summary>
    /// Primary key. Stores the Discord snowflake id of the user so the
    /// application doesn't have to maintain a separate identity column.
    /// </summary>
    public ulong Id { get; set; }

    /// <summary>
    /// The user's Discord username at the time the row was last written.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// JSON-serialized <c>UserSettings</c> blob (color preference, etc.).
    /// Round-tripped via the AutoMapper profile using
    /// <c>VoyagerJsonSettings.Default</c>.
    /// </summary>
    public string Settings { get; set; } = null!;
}