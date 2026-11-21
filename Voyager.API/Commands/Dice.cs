using BusinessLogic.Enums.Types;
using BusinessLogic.Json.Models;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Voyager.API.Commands.Autocompletes;
using Voyager.API.Commands.Handlers;
using Voyager.API.Views;

namespace Voyager.API.Commands;

/// <summary>
/// Command group for dice rolling and saved dice configuration commands
/// (<c>/dice roll</c>, <c>/dice create</c>, <c>/dice delete</c>, <c>/dice guide</c>).
/// </summary>
/// <param name="scopeFactory">
/// Factory used to create a DI scope per interaction so scoped services can be
/// resolved safely.
/// </param>
[SlashCommandGroup("dice", "Commands for rolling and managing dice configurations")]
public class Dice(IServiceScopeFactory scopeFactory) : ApplicationCommandModule
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    /// <summary>
    /// Rolls a saved dice configuration (via autocomplete) or a custom dice
    /// formula, or calculates statistics when <paramref name="statistics"/> is specified.
    /// Public in channels so other players and GMs can see the roll, save it, or clear it.
    /// </summary>
    /// <param name="ctx">The DSharpPlus slash command interaction context.</param>
    /// <param name="dice">The saved dice name or custom dice formula to roll.</param>
    /// <param name="statistics">Optional statistics calculation mode instead of a random roll.</param>
    [SlashCommand("roll", "Roll a saved dice configuration or a custom dice formula")]
    public async Task Roll(
        InteractionContext ctx,
        [Autocomplete(typeof(SavedDiceAutocompleteProvider))]
        [Option("Dice", "Saved dice name or custom dice formula (e.g. 4d6h2+4)", true)] string dice,
        [Option("Statistics", "Calculate statistics instead of rolling randomly")] DiceStatisticsType? statistics = null)
    {
        await ctx.DeferAsync(ephemeral: false);
        await using var scope = _scopeFactory.CreateAsyncScope();
        var handler = await CreateHandlerAsync(scope.ServiceProvider, ctx.Guild?.Id, isEphemeral: false);

        var message = await handler.RollAsync(ctx.User.Id, ctx.User.Username, dice, statistics);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder(message));
    }

    /// <summary>
    /// Creates and saves a new named dice configuration for the caller.
    /// </summary>
    /// <param name="ctx">The DSharpPlus slash command interaction context.</param>
    /// <param name="name">The name for the saved dice configuration.</param>
    /// <param name="formula">The dice formula to save (e.g. <c>4d6h2+4</c>).</param>
    [SlashCommand("create", "Save a new named dice configuration")]
    public async Task Create(
        InteractionContext ctx,
        [Option("Name", "Name for the saved dice configuration")] string name,
        [Option("Formula", "Dice formula to save (e.g. 4d6h2+4)")] string formula)
    {
        bool isServer = ctx.Guild is not null;

        await ctx.DeferAsync(ephemeral: isServer);
        await using var scope = _scopeFactory.CreateAsyncScope();
        var handler = await CreateHandlerAsync(scope.ServiceProvider, ctx.Guild?.Id, isEphemeral: isServer);

        var message = await handler.CreateAsync(ctx.User.Id, name, formula);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder(message));
    }

    /// <summary>
    /// Deletes one of the caller's saved dice configurations by name.
    /// </summary>
    /// <param name="ctx">The DSharpPlus slash command interaction context.</param>
    /// <param name="name">The name of the saved dice configuration to delete.</param>
    [SlashCommand("delete", "Delete a saved dice configuration")]
    public async Task Delete(
        InteractionContext ctx,
        [Autocomplete(typeof(SavedDiceAutocompleteProvider))]
        [Option("Name", "Name of the saved dice configuration to delete", true)] string name)
    {
        bool isServer = ctx.Guild is not null;

        await ctx.DeferAsync(ephemeral: isServer);
        await using var scope = _scopeFactory.CreateAsyncScope();
        var handler = await CreateHandlerAsync(scope.ServiceProvider, ctx.Guild?.Id, isEphemeral: isServer);

        var message = await handler.DeleteAsync(ctx.User.Id, name);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder(message));
    }

    /// <summary>
    /// Displays an ephemeral guide explaining Voyager's dice roll syntax, rules, and examples.
    /// </summary>
    /// <param name="ctx">The DSharpPlus slash command interaction context.</param>
    [SlashCommand("guide", "View a guide on dice roll syntax, tags, and statistics")]
    public async Task Guide(InteractionContext ctx)
    {
        bool isServer = ctx.Guild is not null;

        await ctx.DeferAsync(ephemeral: isServer);
        await using var scope = _scopeFactory.CreateAsyncScope();
        var handler = await CreateHandlerAsync(scope.ServiceProvider, ctx.Guild?.Id, isEphemeral: isServer);

        var message = handler.Guide();
        await ctx.EditResponseAsync(new DiscordWebhookBuilder(message));
    }

    /// <summary>
    /// Resolves scoped services from <paramref name="services"/> and constructs
    /// a <see cref="DiceHandler"/> configured for the current interaction's
    /// server settings and ephemerality.
    /// </summary>
    private static async Task<DiceHandler> CreateHandlerAsync(
        IServiceProvider services,
        ulong? guildId,
        bool isEphemeral)
    {
        var diceRollService = services.GetRequiredService<IDiceRollService>();
        var userService = services.GetRequiredService<IUserService>();
        var serverService = services.GetRequiredService<IServerService>();
        var enumService = services.GetRequiredService<IEnumService>();

        ServerSettings settings = new();
        if (guildId.HasValue)
        {
            var server = await serverService.GetByIdAsync(guildId.Value);
            if (server is not null)
            {
                settings = server.Settings;
            }
        }

        var viewFactory = new ViewFactory(isEphemeral, enumService, settings);
        return new DiceHandler(diceRollService, userService, viewFactory);
    }
}
