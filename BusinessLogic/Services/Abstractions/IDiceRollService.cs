using BusinessLogic.Dtos.DiceRoll;
using BusinessLogic.Enums.Types;
using BusinessLogic.Services.Generic;
using Repository.Models;

namespace BusinessLogic.Services.Abstractions;

/// <summary>
/// Service abstraction for managing saved user dice roll configurations and
/// evaluating dice roll formulas and statistics.
/// </summary>
public interface IDiceRollService
    : IBaseService<ulong, DiceRoll, DiceRollReadDto, DiceRollCreateDto, DiceRollUpdateDto>
{
    /// <summary>
    /// Maximum number of saved dice configurations a single user may have.
    /// Matches Discord's 25-option autocomplete limit.
    /// </summary>
    public const int MaxSavedDicePerUser = 25;

    /// <summary>
    /// Retrieves all saved dice roll configurations belonging to a specific user.
    /// </summary>
    /// <param name="userId">The Discord snowflake ID of the user.</param>
    /// <returns>All saved dice configurations owned by <paramref name="userId"/>.</returns>
    public Task<IEnumerable<DiceRollReadDto>> GetByUserIdAsync(ulong userId);

    /// <summary>
    /// Retrieves a saved dice roll configuration by its owning user and name.
    /// </summary>
    /// <param name="userId">The Discord snowflake ID of the user.</param>
    /// <param name="name">The name of the saved dice configuration.</param>
    /// <returns>The matching <see cref="DiceRollReadDto"/>, or <c>null</c> if not found.</returns>
    public Task<DiceRollReadDto?> GetByUserAndNameAsync(ulong userId, string name);

    /// <summary>
    /// Retrieves a saved dice roll configuration by its owning user and formula.
    /// </summary>
    /// <param name="userId">The Discord snowflake ID of the user.</param>
    /// <param name="formula">The formula of the saved dice configuration.</param>
    /// <returns>The matching <see cref="DiceRollReadDto"/>, or <c>null</c> if not found.</returns>
    public Task<DiceRollReadDto?> GetByUserAndFormulaAsync(ulong userId, string formula);

    /// <summary>
    /// Validates whether a dice roll formula string is syntactically valid
    /// and within engine limits.
    /// </summary>
    /// <param name="formula">The raw dice formula string to validate.</param>
    /// <param name="normalizedFormula">When valid, receives the normalized formula string.</param>
    /// <param name="errorMessage">When invalid, receives a human-readable error message.</param>
    /// <returns><c>true</c> if the formula is valid; otherwise <c>false</c>.</returns>
    public bool TryValidateFormula(string? formula, out string normalizedFormula, out string errorMessage);

    /// <summary>
    /// Parses and evaluates a dice formula string, computing both a random roll
    /// and exact statistics (Minimum, Maximum, Average, Standard Deviation) for
    /// every term, section, tag, and the overall total.
    /// </summary>
    /// <param name="formula">The dice formula string to evaluate.</param>
    /// <param name="statisticsType">Optional statistics mode requested by the user, or <c>null</c> for a random roll.</param>
    /// <param name="random">Optional random number generator (defaults to <see cref="Random.Shared"/>; injectable for deterministic unit tests).</param>
    /// <returns>The full <see cref="DiceEvaluationResultDto"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="formula"/> is invalid.</exception>
    public DiceEvaluationResultDto Evaluate(
        string formula,
        DiceStatisticsType? statisticsType = null,
        Random? random = null);
}
