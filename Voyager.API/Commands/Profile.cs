using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

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
        (var mediaService, var userService) = GetScopeServices(scope);

        var user = await userService.GetByIdAsync(ctx.User.Id);
        if (user == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder(new DiscordMessageBuilder()
            .AddEmbed(new DiscordEmbedBuilder
            {
                Title = $"Error: User not found.",
                Color = DiscordColor.Red
            })));

            return;
        }

        await ctx.EditResponseAsync(new DiscordWebhookBuilder(new DiscordMessageBuilder()
            .AddEmbed(new DiscordEmbedBuilder
            {
                Title = $"{user.Name}'s Profile",
                Color = mediaService.ConvertColor(user.Settings.Color)
            })));
    }

    private static (IMediaSerivce, IUserService) GetScopeServices(AsyncServiceScope scope) =>
        (scope.ServiceProvider.GetRequiredService<IMediaSerivce>(),
         scope.ServiceProvider.GetRequiredService<IUserService>());
}
