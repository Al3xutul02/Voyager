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
        // Defer interaction — gives us up to 15 minutes to reply
        await ctx.DeferAsync();
        var handler = await CreateHandlerAsync();

        var message = await handler.ViewAsync(user);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder(message));
    }

    [SlashCommand("create", "Create your user profile")]
    public async Task Create(InteractionContext ctx,
        [Option("Color", "The color for your profile")] DiscordColor? color)
    {
        await ctx.DeferAsync();
        var handler = await CreateHandlerAsync();

        var message = await handler.CreateAsync(ctx.User, color);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder(message));
    }

    private async Task<ProfileHandler> CreateHandlerAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var enumService = scope.ServiceProvider.GetRequiredService<IEnumService>();

        // The command stays a thin adapter: it only translates the Discord
        // interaction into primitives and hands off to ProfileHandler, which
        // holds the testable logic.
        var viewFactory = new ViewFactory(enumService, new ServerSettings());
        return new ProfileHandler(userService, enumService, viewFactory);
    }
}
