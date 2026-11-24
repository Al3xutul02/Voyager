namespace Repository.Models;

/// <summary>
/// Database model for a Pathfinder 2e game/campaign hosted on a server in Normal mode.
/// </summary>
public class Game
{
    /// <summary>
    /// Auto-generated primary key for the game.
    /// </summary>
    public ulong Id { get; set; }

    /// <summary>
    /// Foreign key referencing the hosting <see cref="Server"/> (<c>servers.Id</c>).
    /// </summary>
    public ulong ServerId { get; set; }

    /// <summary>
    /// Navigation property to the hosting <see cref="Server"/>.
    /// </summary>
    public Server Server { get; set; } = null!;

    /// <summary>
    /// Foreign key referencing the Game Master <see cref="User"/> (<c>users.Id</c>) who created the game.
    /// </summary>
    public ulong GameMasterId { get; set; }

    /// <summary>
    /// Navigation property to the Game Master <see cref="User"/>.
    /// </summary>
    public User GameMaster { get; set; } = null!;

    /// <summary>
    /// Name of the game (unique per active server).
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Description or synopsis of the game.
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// Palette color used for the game's embeds (stored as enum integer value).
    /// </summary>
    public int Color { get; set; }

    /// <summary>
    /// Relative file path on disk where the GM token image is stored.
    /// </summary>
    public string GmTokenImagePath { get; set; } = null!;

    /// <summary>
    /// Relative file path on disk where the game's thumbnail image is stored.
    /// </summary>
    public string ThumbnailImagePath { get; set; } = null!;

    /// <summary>
    /// Discord snowflake ID of the channel category created for the game.
    /// </summary>
    public ulong CategoryId { get; set; }

    /// <summary>
    /// Discord snowflake ID of the game's private <c>gm-channel</c>.
    /// </summary>
    public ulong GmChannelId { get; set; }

    /// <summary>
    /// Discord snowflake ID of the game's <c>general</c> player talk channel.
    /// </summary>
    public ulong GeneralChannelId { get; set; }

    /// <summary>
    /// Discord snowflake ID of the game's <c>characters</c> channel.
    /// </summary>
    public ulong CharactersChannelId { get; set; }

    /// <summary>
    /// Discord snowflake ID of the game's private <c>npcs</c> channel.
    /// </summary>
    public ulong NpcsChannelId { get; set; }

    /// <summary>
    /// UTC timestamp when the game record was first created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC timestamp when the game record was last modified.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional UTC timestamp when the game record was soft-deleted, or <c>null</c> if active.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Players enrolled in this game.
    /// </summary>
    public ICollection<GamePlayer> Players { get; set; } = [];
}
