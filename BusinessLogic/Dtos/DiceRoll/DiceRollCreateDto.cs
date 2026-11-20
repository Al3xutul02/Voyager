namespace BusinessLogic.Dtos.DiceRoll;

/// <summary>
/// Data transfer object for creating a new saved dice roll configuration.
/// </summary>
/// <param name="UserId">The Discord snowflake ID of the user saving the roll.</param>
/// <param name="Name">The user-chosen name for the saved dice configuration.</param>
/// <param name="Formula">The dice roll formula string.</param>
public record DiceRollCreateDto(ulong UserId, string Name, string Formula);
