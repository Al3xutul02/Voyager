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

    [SlashCommand("view", "See your user profile")]
    public async Task View(InteractionContext ctx)
    {
        // Defer interaction — gives us up to 15 minutes to reply
        await ctx.DeferAsync();

        await using var scope = _scopeFactory.CreateAsyncScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var enumService = scope.ServiceProvider.GetRequiredService<IEnumService>();

        // The command stays a thin adapter: it only translates the Discord
        // interaction into primitives and hands off to ProfileHandler, which
        // holds the testable logic.
        var viewFactory = new ViewFactory(enumService, new ServerSettings());
        var handler = new ProfileHandler(userService, viewFactory);

        var message = await handler.BuildProfileViewAsync(ctx.User.Id);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder(message));
    }
}
