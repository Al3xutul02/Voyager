using BusinessLogic.Enums.Types;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace Voyager.API.Commands.Autocompletes;

/// <summary>
/// Slash-command autocomplete provider for <see cref="Color"/> palette names.
/// Filters all defined <see cref="Color"/> enum names case-insensitively by
/// what the user has typed so far and returns up to Discord's 25-choice limit.
/// </summary>
public class ColorAutocompleteProvider : IAutocompleteProvider
{
    private static readonly string[] AllColorNames = Enum.GetNames<Color>();

    /// <summary>
    /// Supplies matching <see cref="DiscordAutoCompleteChoice"/> items to
    /// DSharpPlus for the active autocomplete interaction.
    /// </summary>
    /// <param name="ctx">The DSharpPlus autocomplete interaction context.</param>
    public Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
    {
        var input = ctx.OptionValue?.ToString();
        return Task.FromResult(GetMatchingChoices(input));
    }

    /// <summary>
    /// Filters the <see cref="Color"/> palette names against <paramref name="input"/>
    /// (case-insensitive substring match) and caps the result at 25 choices,
    /// which is Discord's maximum per autocomplete response. Extracted as a
    /// static method so it can be unit-tested without constructing DSharpPlus's
    /// non-mockable <see cref="AutocompleteContext"/>.
    /// </summary>
    /// <param name="input">Partial color name typed by the user, or <c>null</c>/empty.</param>
    /// <returns>Up to 25 matching <see cref="DiscordAutoCompleteChoice"/> items.</returns>
    public static IEnumerable<DiscordAutoCompleteChoice> GetMatchingChoices(string? input)
    {
        IEnumerable<string> matches = string.IsNullOrWhiteSpace(input)
            ? AllColorNames
            : AllColorNames.Where(name => name.Contains(input.Trim(), StringComparison.OrdinalIgnoreCase));

        return matches
            .Take(25)
            .Select(name => new DiscordAutoCompleteChoice(name, name));
    }
}
