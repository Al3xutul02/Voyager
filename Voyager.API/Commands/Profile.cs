using BusinessLogic.Json.Models;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Voyager.API.Enums.Types;
using Voyager.API.Views;

namespace Voyager.API.Commands;

/// <summary>
/// Command group for profile related commands.
/// </summary>
/// <param name="scopeFactory">
/// Factory used to create a DI scope per interaction. We can't inject scoped
/// services (like <see cref="IUserService"/>) directly because DSharpPlus
/// instantiates command modules from the root provider, which fails scope
/// validation. Instead we resolve scoped services inside each command method.
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
        (var enumService, var userService) = GetScopeServices(scope);
        var viewFactory = new ViewFactory(enumService, new ServerSettings());

        var user = await userService.GetByIdAsync(ctx.User.Id);
        if (user == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder(viewFactory.CreateNotification(
                NotificationType.Error,
                $"Error: User not found.")));

            return;
        }

        await ctx.EditResponseAsync(new DiscordWebhookBuilder(viewFactory.CreateUserProfile(user)));
    }

    private static (IEnumSerivce, IUserService) GetScopeServices(AsyncServiceScope scope) =>
        (scope.ServiceProvider.GetRequiredService<IEnumSerivce>(),
         scope.ServiceProvider.GetRequiredService<IUserService>());
}
