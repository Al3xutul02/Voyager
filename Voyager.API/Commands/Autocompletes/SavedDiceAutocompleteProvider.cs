using BusinessLogic.Dtos.DiceRoll;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace Voyager.API.Commands.Autocompletes;

/// <summary>
/// Slash-command autocomplete provider for a user's saved dice configurations.
/// Queries the user's saved rolls via <see cref="IDiceRollService"/> and filters
/// them case-insensitively by what the user has typed so far, up to Discord's
/// 25-choice limit.
/// </summary>
public class SavedDiceAutocompleteProvider : IAutocompleteProvider
{
    /// <summary>
    /// Supplies matching <see cref="DiscordAutoCompleteChoice"/> items to
    /// DSharpPlus for the active autocomplete interaction.
    /// </summary>
    /// <param name="ctx">The DSharpPlus autocomplete interaction context.</param>
    public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
    {
        var scopeFactory = ctx.Services.GetService<IServiceScopeFactory>();
        if (scopeFactory is null)
        {
            return [];
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var diceRollService = scope.ServiceProvider.GetRequiredService<IDiceRollService>();
        var savedRolls = await diceRollService.GetByUserIdAsync(ctx.User.Id);
        var input = ctx.OptionValue?.ToString();

        return GetMatchingChoices(savedRolls, input);
    }

    /// <summary>
    /// Filters <paramref name="savedRolls"/> against <paramref name="input"/>
    /// (case-insensitive substring match on name or formula) and caps the
    /// result at 25 choices. Extracted as a static method so it can be
    /// unit-tested without constructing DSharpPlus's non-mockable
    /// <see cref="AutocompleteContext"/>.
    /// </summary>
    /// <param name="savedRolls">The user's saved dice configurations.</param>
    /// <param name="input">Partial text typed by the user, or <c>null</c>/empty.</param>
    /// <returns>Up to 25 matching <see cref="DiscordAutoCompleteChoice"/> items.</returns>
    public static IEnumerable<DiscordAutoCompleteChoice> GetMatchingChoices(
        IEnumerable<DiceRollReadDto> savedRolls,
        string? input)
    {
        IEnumerable<DiceRollReadDto> matches = string.IsNullOrWhiteSpace(input)
            ? savedRolls
            : savedRolls.Where(r =>
                r.Name.Contains(input.Trim(), StringComparison.OrdinalIgnoreCase) ||
                r.Formula.Contains(input.Trim(), StringComparison.OrdinalIgnoreCase));

        return matches
            .Take(IDiceRollService.MaxSavedDicePerUser)
            .Select(r => new DiscordAutoCompleteChoice($"{r.Name} ({r.Formula})", r.Name));
    }
}
