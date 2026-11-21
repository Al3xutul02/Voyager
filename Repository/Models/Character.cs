namespace Repository.Models;

/// <summary>
/// Database model for character information.
/// </summary>
public class Character
{
    /// <summary>
    /// Auto-generated primary key for the character.
    /// </summary>
    public ulong Id { get; set; }

    /// <summary>
    /// Foreign key referencing the owning <see cref="User"/> (<c>users.Id</c>).
    /// </summary>
    public ulong UserId { get; set; }

    /// <summary>
    /// Navigation property to the owning <see cref="User"/>.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// User-defined or imported name of the character.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Optional character backstory or biography.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Palette color used for the character's embeds (stored as enum integer value).
    /// </summary>
    public int Color { get; set; }

    /// <summary>
    /// Relative file path on disk where the token image is stored.
    /// </summary>
    public string TokenImagePath { get; set; } = null!;

    /// <summary>
    /// Relative file path on disk where the actor portrait image is stored.
    /// </summary>
    public string ActorImagePath { get; set; } = null!;

    /// <summary>
    /// The current game/campaign the character is in, or <c>null</c> if not in a game.
    /// </summary>
    public string? CurrentGame { get; set; }

    /// <summary>
    /// Character's total currency evaluated in gold pieces (GP).
    /// </summary>
    public decimal Gold { get; set; }

    /// <summary>
    /// Optional Pathbuilder 2e export JSON ID.
    /// </summary>
    public string? PathbuilderId { get; set; }

    /// <summary>
    /// JSON-serialized character details (ancestry, class, level, age, gender).
    /// </summary>
    public string Details { get; set; } = null!;

    /// <summary>
    /// UTC timestamp when the character record was first created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC timestamp when the character record was last modified.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional UTC timestamp when the character record was soft-deleted, or <c>null</c> if active.
    /// </summary>
    public DateTime? DeletedAt { get; set; }
}
