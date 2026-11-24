namespace Repository.Models;

/// <summary>
/// Join entity linking a <see cref="User"/> (player) to a <see cref="Game"/>.
/// </summary>
public class GamePlayer
{
    /// <summary>
    /// Foreign key referencing the <see cref="Game"/> (<c>games.Id</c>).
    /// </summary>
    public ulong GameId { get; set; }

    /// <summary>
    /// Navigation property to the <see cref="Game"/>.
    /// </summary>
    public Game Game { get; set; } = null!;

    /// <summary>
    /// Foreign key referencing the player <see cref="User"/> (<c>users.Id</c>).
    /// </summary>
    public ulong UserId { get; set; }

    /// <summary>
    /// Navigation property to the player <see cref="User"/>.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// UTC timestamp when the player accepted the invite and joined the game.
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
