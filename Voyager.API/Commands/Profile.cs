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
/// Command group for profile related commands (<c>/profile view</c>,
/// <c>/profile create</c>, <c>/profile change</c>).
/// </summary>
/// <param name="scopeFactory">
/// Factory used to create a DI scope per interaction. We can't inject scoped
/// services directly because DSharpPlus instantiates command modules from the
/// root provider, which fails scope validation. Instead we resolve scoped
/// services inside each command method.
/// </param>
[SlashCommandGroup("profile", "Commands regarding discord users")]
public class Profile(IServiceScopeFactory scopeFactory) : ApplicationCommandModule
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    /// <summary>
    /// Displays a user's profile card. When the caller views their own
    /// profile, a dropdown select menu is attached to switch between the
    /// Profile and Settings pages.
    /// </summary>
    /// <param name="ctx">The DSharpPlus slash command interaction context.</param>
    /// <param name="user">The Discord user whose profile to view.</param>
    [SlashCommand("view", "See a user profile")]
    public async Task View(InteractionContext ctx,
        [Option("User", "The user whose profile to view")] DiscordUser user)
    {
        bool isServer = ctx.Guild is not null;

        // Defer interaction — gives us up to 15 minutes to reply
        await ctx.DeferAsync(ephemeral: isServer);
        await using var scope = _scopeFactory.CreateAsyncScope();
        var handler = CreateHandler(scope.ServiceProvider, isServer);

        var message = await handler.ViewAsync(user.Id, ctx.User.Id);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder(message));
    }

    /// <summary>
    /// Creates a new Voyager profile for the caller with an optional initial
    /// palette color supplied via autocomplete.
    /// </summary>
    /// <param name="ctx">The DSharpPlus slash command interaction context.</param>
    /// <param name="color">Optional palette color name for the new profile.</param>
    [SlashCommand("create", "Create your user profile")]
    public async Task Create(InteractionContext ctx,
        [Autocomplete(typeof(ColorAutocompleteProvider))]
        [Option("Color", "The color for your profile", true)] string? color = null)
    {
        bool isServer = ctx.Guild is not null;

        await ctx.DeferAsync(ephemeral: isServer);
        await using var scope = _scopeFactory.CreateAsyncScope();
        var handler = CreateHandler(scope.ServiceProvider, isServer);

        var message = await handler.CreateAsync(ctx.User.Id, ctx.User.Username, color);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder(message));
    }

    /// <summary>
    /// Updates one or more of the caller's user settings (profile color,
    /// game privacy, character privacy). Shows a warning if no options are
    /// provided.
    /// </summary>
    /// <param name="ctx">The DSharpPlus slash command interaction context.</param>
    /// <param name="color">Optional new palette color name for the profile.</param>
    /// <param name="gamePrivacy">Optional new privacy setting for games.</param>
    /// <param name="characterPrivacy">Optional new privacy setting for characters.</param>
    [SlashCommand("change", "Change your user settings")]
    public async Task Change(InteractionContext ctx,
        [Autocomplete(typeof(ColorAutocompleteProvider))]
        [Option("Color", "The color for your profile", true)] string? color = null,
        [Option("GamePrivacy", "Privacy setting for your games")] PrivacyOption? gamePrivacy = null,
        [Option("CharacterPrivacy", "Privacy setting for your characters")] PrivacyOption? characterPrivacy = null)
    {
        bool isServer = ctx.Guild is not null;

        await ctx.DeferAsync(ephemeral: isServer);
        await using var scope = _scopeFactory.CreateAsyncScope();
        var handler = CreateHandler(scope.ServiceProvider, isServer);

        var message = await handler.ChangeAsync(ctx.User.Id, ctx.User.Username, color, gamePrivacy, characterPrivacy);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder(message));
    }

    /// <summary>
    /// Resolves scoped services from <paramref name="services"/> and constructs
    /// a <see cref="ProfileHandler"/> configured for the current interaction's
    /// ephemerality.
    /// </summary>
    /// <param name="services">The active per-interaction scoped service provider.</param>
    /// <param name="isEphemeral">Whether the interaction response is ephemeral.</param>
    private static ProfileHandler CreateHandler(IServiceProvider services, bool isEphemeral)
    {
        var userService = services.GetRequiredService<IUserService>();
        var enumService = services.GetRequiredService<IEnumService>();

        // The command stays a thin adapter: it only translates the Discord
        // interaction into primitives and hands off to ProfileHandler, which
        // holds the testable logic.
        var viewFactory = new ViewFactory(isEphemeral, enumService, new ServerSettings());
        return new ProfileHandler(userService, enumService, viewFactory);
    }
}
