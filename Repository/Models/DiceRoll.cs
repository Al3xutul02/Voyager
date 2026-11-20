namespace Repository.Models;

/// <summary>
/// Database model for a saved user dice roll configuration.
/// </summary>
public class DiceRoll
{
    /// <summary>
    /// Auto-generated primary key for the saved dice roll.
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
    /// User-defined name for the saved dice configuration.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// The dice roll expression/formula (e.g. <c>4d6h2+4</c> or <c>(1d6)[fire]+4[poison]</c>).
    /// </summary>
    public string Formula { get; set; } = null!;

    /// <summary>
    /// UTC timestamp when the saved dice record was first created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC timestamp when the saved dice record was last modified.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional UTC timestamp when the saved dice record was soft-deleted, or <c>null</c> if active.
    /// </summary>
    public DateTime? DeletedAt { get; set; }
}
