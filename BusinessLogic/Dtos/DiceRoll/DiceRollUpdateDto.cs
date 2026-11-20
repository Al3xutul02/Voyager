namespace BusinessLogic.Dtos.DiceRoll;

/// <summary>
/// Data transfer object for updating an existing saved dice roll configuration.
/// </summary>
/// <param name="Id">The primary key ID of the saved dice roll record to update.</param>
/// <param name="UserId">The Discord snowflake ID of the user who owns this saved roll.</param>
/// <param name="Name">The updated name for the saved dice configuration.</param>
/// <param name="Formula">The updated dice roll formula string.</param>
public record DiceRollUpdateDto(ulong Id, ulong UserId, string Name, string Formula);
