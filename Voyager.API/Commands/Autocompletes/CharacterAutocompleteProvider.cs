using BusinessLogic.Dtos.Character;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace Voyager.API.Commands.Autocompletes;

/// <summary>
/// Slash-command autocomplete provider for a user's characters.
/// Queries the user's characters via <see cref="ICharacterService"/> and filters
/// them case-insensitively by character name.
/// </summary>
public class CharacterAutocompleteProvider : IAutocompleteProvider
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
        var characterService = scope.ServiceProvider.GetRequiredService<ICharacterService>();
        var characters = await characterService.GetByUserIdAsync(ctx.User.Id);
        var input = ctx.OptionValue?.ToString();

        return GetMatchingChoices(characters, input);
    }

    /// <summary>
    /// Filters <paramref name="characters"/> against <paramref name="input"/>
    /// (case-insensitive substring match on name) and formats the choice name
    /// as "Name (Ancestry Class Level)" with the character name as value.
    /// Extracted as a static method so it can be unit-tested without DSharpPlus contexts.
    /// </summary>
    /// <param name="characters">The user's characters.</param>
    /// <param name="input">Partial text typed by the user, or <c>null</c>/empty.</param>
    /// <returns>Up to 10 matching <see cref="DiscordAutoCompleteChoice"/> items.</returns>
    public static IEnumerable<DiscordAutoCompleteChoice> GetMatchingChoices(
        IEnumerable<CharacterReadDto> characters,
        string? input)
    {
        IEnumerable<CharacterReadDto> matches = string.IsNullOrWhiteSpace(input)
            ? characters
            : characters.Where(c => c.Name.Contains(input.Trim(), StringComparison.OrdinalIgnoreCase));

        return matches
            .Take(ICharacterService.MaxCharactersPerUser)
            .Select(c =>
            {
                var ancestry = string.IsNullOrWhiteSpace(c.Details.Ancestry) ? "Unknown" : c.Details.Ancestry;
                var cls = string.IsNullOrWhiteSpace(c.Details.Class) ? "Unknown" : c.Details.Class;
                var lvl = string.IsNullOrWhiteSpace(c.Details.Level) ? "1" : c.Details.Level;
                return new DiscordAutoCompleteChoice($"{c.Name} ({ancestry} {cls} {lvl})", c.Name);
            });
    }
}
