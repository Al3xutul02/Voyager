namespace BusinessLogic.Dtos.DiceRoll;

/// <summary>
/// Data transfer object for reading a saved dice roll configuration.
/// </summary>
/// <param name="Id">The primary key ID of the saved dice roll record.</param>
/// <param name="UserId">The Discord snowflake ID of the user who owns this saved roll.</param>
/// <param name="Name">The user-chosen name for the saved dice configuration.</param>
/// <param name="Formula">The dice roll formula string.</param>
public record DiceRollReadDto(ulong Id, ulong UserId, string Name, string Formula);
