using BusinessLogic.Dtos.DiceRoll;
using BusinessLogic.Enums.Types;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;
using Voyager.API.Enums.Types;
using Voyager.API.Views;

namespace Voyager.API.Commands.Handlers;

/// <summary>
/// Testable core of the <c>/dice</c> command group (<c>roll</c>, <c>create</c>,
/// <c>delete</c>, <c>guide</c>) and the "Save Roll" button/modal flow.
/// Takes primitive inputs and returns a <see cref="DiscordMessageBuilder"/> so
/// all branching logic can be unit-tested without DSharpPlus's non-mockable
/// interaction contexts.
/// </summary>
/// <param name="diceRollService">Manages saved dice configurations and evaluates dice formulas.</param>
/// <param name="userService">Looks up user profiles and color preferences.</param>
/// <param name="viewFactory">Builds the Discord message for the response.</param>
public class DiceHandler(
    IDiceRollService diceRollService,
    IUserService userService,
    ViewFactory viewFactory)
{
    private readonly IDiceRollService _diceRollService = diceRollService;
    private readonly IUserService _userService = userService;
    private readonly ViewFactory _viewFactory = viewFactory;

    /// <summary>
    /// Builds the response for <c>/dice roll</c>: first checks if
    /// <paramref name="diceInput"/> matches a saved dice configuration owned by
    /// <paramref name="callerId"/>; if not, validates and evaluates
    /// <paramref name="diceInput"/> as a custom dice formula. Users do not need
    /// a profile to roll custom dice formulas.
    /// </summary>
    /// <param name="callerId">The Discord snowflake ID of the user rolling the dice.</param>
    /// <param name="callerName">The Discord username of the user rolling the dice.</param>
    /// <param name="diceInput">Either the name of a saved dice configuration or a custom dice formula.</param>
    /// <param name="statistics">Optional statistics calculation mode; <c>null</c> performs a random roll.</param>
    /// <param name="random">Optional RNG override for deterministic testing.</param>
    public async Task<DiscordMessageBuilder> RollAsync(
        ulong callerId,
        string callerName,
        string diceInput,
        DiceStatisticsType? statistics = null,
        Random? random = null)
    {
        if (string.IsNullOrWhiteSpace(diceInput))
        {
            return _viewFactory.CreateNotification(NotificationType.Error, "Error: Dice formula cannot be empty.");
        }

        string trimmedInput = diceInput.Trim();
        var savedRoll = await _diceRollService.GetByUserAndNameAsync(callerId, trimmedInput);

        string formulaToEvaluate;
        string? savedRollName = null;

        if (savedRoll is not null)
        {
            formulaToEvaluate = savedRoll.Formula;
            savedRollName = savedRoll.Name;
        }
        else
        {
            if (!_diceRollService.TryValidateFormula(trimmedInput, out string normalizedFormula, out string errorMessage))
            {
                return _viewFactory.CreateNotification(NotificationType.Error, errorMessage);
            }

            formulaToEvaluate = normalizedFormula;
        }

        try
        {
            var evaluation = _diceRollService.Evaluate(formulaToEvaluate, statistics, random);
            var userProfile = await _userService.GetByIdAsync(callerId);
            Color? userColor = userProfile?.Settings.Color;

            return _viewFactory.CreateDiceRoll(callerId, callerName, evaluation, userColor, savedRollName);
        }
        catch (ArgumentException ex)
        {
            return _viewFactory.CreateNotification(NotificationType.Error, ex.Message);
        }
        catch (Exception)
        {
            return _viewFactory.CreateNotification(NotificationType.Error, "Error: An error occurred while rolling the dice.");
        }
    }

    /// <summary>
    /// Validates whether a user can save a dice formula before prompting for a name
    /// (used by the "Save" button under a roll message, following the activity diagram's
    /// "Already Saved?" and "Is The Create Limit Reached?" checks).
    /// </summary>
    /// <param name="callerId">The Discord snowflake ID of the user clicking Save.</param>
    /// <param name="formula">The dice formula extracted from the roll message.</param>
    /// <returns><c>null</c> if the user may proceed to the name modal, or an error/warning <see cref="DiscordMessageBuilder"/> if blocked.</returns>
    public async Task<DiscordMessageBuilder?> ValidateCanSaveAsync(ulong callerId, string? formula)
    {
        var user = await _userService.GetByIdAsync(callerId);
        if (user is null)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: You must create a profile first using /profile create before saving dice configurations.");
        }

        if (!_diceRollService.TryValidateFormula(formula, out string normalizedFormula, out string errorMessage))
        {
            return _viewFactory.CreateNotification(NotificationType.Error, errorMessage);
        }

        var existingByFormula = await _diceRollService.GetByUserAndFormulaAsync(callerId, normalizedFormula);
        if (existingByFormula is not null)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Warning,
                $"Warning: This dice formula is already saved as '{existingByFormula.Name}'.");
        }

        var userRolls = (await _diceRollService.GetByUserIdAsync(callerId)).ToList();
        if (userRolls.Count >= IDiceRollService.MaxSavedDicePerUser)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                $"Error: You have reached the maximum limit of {IDiceRollService.MaxSavedDicePerUser} saved dice configurations.");
        }

        return null;
    }

    /// <summary>
    /// Builds the response for <c>/dice create</c> (and the Save Roll modal submission):
    /// verifies the user has a profile, validates the formula and name, checks for
    /// duplicate formulas/names and the 25-roll create limit, and persists the new
    /// <see cref="DiceRollCreateDto"/>.
    /// </summary>
    /// <param name="callerId">The Discord snowflake ID of the user saving the dice configuration.</param>
    /// <param name="name">The user-chosen name for the saved dice configuration.</param>
    /// <param name="formula">The dice formula to save.</param>
    public async Task<DiscordMessageBuilder> CreateAsync(ulong callerId, string name, string formula)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return _viewFactory.CreateNotification(NotificationType.Error, "Error: Dice configuration name cannot be empty.");
        }

        string trimmedName = name.Trim();
        if (trimmedName.Length > 100)
        {
            return _viewFactory.CreateNotification(NotificationType.Error, "Error: Dice configuration name cannot exceed 100 characters.");
        }

        var preCheckError = await ValidateCanSaveAsync(callerId, formula);
        if (preCheckError is not null)
        {
            return preCheckError;
        }

        _diceRollService.TryValidateFormula(formula, out string normalizedFormula, out _);

        var existingByName = await _diceRollService.GetByUserAndNameAsync(callerId, trimmedName);
        if (existingByName is not null)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Warning,
                $"Warning: You already have a saved dice configuration named '{trimmedName}'.");
        }

        try
        {
            await _diceRollService.CreateAsync(new DiceRollCreateDto(callerId, trimmedName, normalizedFormula));
            return _viewFactory.CreateNotification(
                NotificationType.Success,
                $"Saved dice configuration '{trimmedName}' (`{normalizedFormula}`) successfully.");
        }
        catch (Exception)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: An error occurred while saving the dice configuration.");
        }
    }

    /// <summary>
    /// Builds the response for <c>/dice delete</c>: verifies the user has a profile,
    /// locates their saved dice configuration by <paramref name="name"/>, and deletes it.
    /// </summary>
    /// <param name="callerId">The Discord snowflake ID of the user deleting the configuration.</param>
    /// <param name="name">The name of the saved dice configuration to delete.</param>
    public async Task<DiscordMessageBuilder> DeleteAsync(ulong callerId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return _viewFactory.CreateNotification(NotificationType.Error, "Error: Dice configuration name cannot be empty.");
        }

        var user = await _userService.GetByIdAsync(callerId);
        if (user is null)
        {
            return _viewFactory.CreateNotification(NotificationType.Error, "Error: User profile not found.");
        }

        string trimmedName = name.Trim();
        var existing = await _diceRollService.GetByUserAndNameAsync(callerId, trimmedName);
        if (existing is null)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                $"Error: Saved dice configuration '{trimmedName}' was not found.");
        }

        try
        {
            await _diceRollService.DeleteAsync(existing.Id);
            return _viewFactory.CreateNotification(
                NotificationType.Success,
                $"Deleted saved dice configuration '{existing.Name}'.");
        }
        catch (Exception)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: An error occurred while deleting the dice configuration.");
        }
    }

    /// <summary>
    /// Builds the response for <c>/dice guide</c>: returns the dice roll syntax and rules guide embed.
    /// </summary>
    public DiscordMessageBuilder Guide() => _viewFactory.CreateDiceGuide();
}
