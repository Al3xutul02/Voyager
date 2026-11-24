using BusinessLogic.Dtos.Game;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace Voyager.API.Commands.Autocompletes;

/// <summary>
/// Slash-command autocomplete provider for games on the current Discord server.
/// Queries the server's active games via <see cref="IGameService"/> and filters
/// them case-insensitively by game name.
/// </summary>
public class GameAutocompleteProvider : IAutocompleteProvider
{
    /// <summary>
    /// Supplies matching <see cref="DiscordAutoCompleteChoice"/> items to
    /// DSharpPlus for the active autocomplete interaction.
    /// </summary>
    /// <param name="ctx">The DSharpPlus autocomplete interaction context.</param>
    public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
    {
        if (ctx.Guild is null)
        {
            return [];
        }

        var scopeFactory = ctx.Services.GetService<IServiceScopeFactory>();
        if (scopeFactory is null)
        {
            return [];
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();
        var games = await gameService.GetByServerIdAsync(ctx.Guild.Id);
        var input = ctx.OptionValue?.ToString();

        return GetMatchingChoices(games, input);
    }

    /// <summary>
    /// Filters <paramref name="games"/> against <paramref name="input"/>
    /// (case-insensitive substring match on game name) and returns up to
    /// <see cref="IGameService.MaxGamesPerServer"/> choices.
    /// Extracted as a static method so it can be unit-tested without DSharpPlus contexts.
    /// </summary>
    /// <param name="games">The active games on the server.</param>
    /// <param name="input">Partial text typed by the user, or <c>null</c>/empty.</param>
    /// <returns>Up to 5 matching <see cref="DiscordAutoCompleteChoice"/> items.</returns>
    public static IEnumerable<DiscordAutoCompleteChoice> GetMatchingChoices(
        IEnumerable<GameReadDto> games,
        string? input)
    {
        IEnumerable<GameReadDto> matches = string.IsNullOrWhiteSpace(input)
            ? games
            : games.Where(g => g.Name.Contains(input.Trim(), StringComparison.OrdinalIgnoreCase));

        return matches
            .Take(IGameService.MaxGamesPerServer)
            .Select(g => new DiscordAutoCompleteChoice(g.Name, g.Name));
    }
}
