using BusinessLogic.Json.Models;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Voyager.API.Commands.Handlers;
using Voyager.API.Views;

namespace Voyager.API.Commands;

/// <summary>
/// Command group for profile related commands.
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

    [SlashCommand("view", "See a user profile")]
    public async Task View(InteractionContext ctx,
        [Option("User", "The user whose profile to view")] DiscordUser user)
    {
        bool isServer = ctx.Guild is not null;

        // Defer interaction — gives us up to 15 minutes to reply
        await ctx.DeferAsync(ephemeral: isServer);
        var handler = await CreateHandlerAsync(isServer);

        var message = await handler.ViewAsync(user, ctx.User);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder(message));
    }

    [SlashCommand("create", "Create your user profile")]
    public async Task Create(InteractionContext ctx,
        [Option("Color", "The color for your profile")] DiscordColor? color)
    {
        bool isServer = ctx.Guild is not null;

        await ctx.DeferAsync(ephemeral: isServer);
        var handler = await CreateHandlerAsync(isServer);

        var message = await handler.CreateAsync(ctx.User, color);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder(message));
    }

    [SlashCommand("settings", "View your user settings")]
    public async Task Settings(InteractionContext ctx)
    {
        bool isServer = ctx.Guild is not null;

        await ctx.DeferAsync(ephemeral: isServer);
        var handler = await CreateHandlerAsync(isServer);

        var message = await handler.SettingsAsync(ctx.User);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder(message));
    }

    private async Task<ProfileHandler> CreateHandlerAsync(bool isEphemeral)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var enumService = scope.ServiceProvider.GetRequiredService<IEnumService>();

        // The command stays a thin adapter: it only translates the Discord
        // interaction into primitives and hands off to ProfileHandler, which
        // holds the testable logic.
        var viewFactory = new ViewFactory(isEphemeral, enumService, new ServerSettings());
        return new ProfileHandler(userService, enumService, viewFactory);
    }
}
