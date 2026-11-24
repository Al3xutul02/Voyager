namespace Repository.Models;

/// <summary>
/// Database model for a configured Discord server (guild).
/// </summary>
public class Server
{
    /// <summary>
    /// Primary key. Stores the Discord snowflake id of the guild so the
    /// application doesn't have to maintain a separate identity column.
    /// </summary>
    public ulong Id { get; set; }

    /// <summary>
    /// The Discord server's name at the time the row was last written.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Optional description displayed on the server's profile embed.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Discord snowflake id of the server's Game Master role.
    /// </summary>
    public ulong GMRoleId { get; set; }

    /// <summary>
    /// Discord snowflake id of the server's Voyager admin channel.
    /// </summary>
    public ulong AdminChannelId { get; set; }

    /// <summary>
    /// JSON-serialized <c>ServerSettings</c> blob (operating mode, notification colors, etc.).
    /// Round-tripped via the AutoMapper profile using
    /// <c>VoyagerJsonSettings.Default</c>.
    /// </summary>
    public string Settings { get; set; } = null!;

    /// <summary>
    /// UTC timestamp when the server record was first created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC timestamp when the server record was last modified.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional UTC timestamp when the server record was soft-deleted, or <c>null</c> if active.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Games hosted on this server.
    /// </summary>
    public ICollection<Game> Games { get; set; } = [];
}
