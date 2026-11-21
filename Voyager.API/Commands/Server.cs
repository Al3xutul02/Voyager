using BusinessLogic.Enums.Types;
using BusinessLogic.Json.Models;
using BusinessLogic.Services.Abstractions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Voyager.API.Commands.Handlers;
using Voyager.API.Enums.Types;
using Voyager.API.Views;

namespace Voyager.API.Commands;

/// <summary>
/// Command group for Discord server (guild) configuration commands
/// (<c>/server setup</c>, <c>/server view</c>, <c>/server change</c>).
/// Restricted to guild channels via <see cref="GuildOnlyAttribute"/>.
/// </summary>
/// <param name="scopeFactory">
/// Factory used to create a DI scope per interaction so scoped services
/// (<see cref="IServerService"/>, <see cref="IEnumService"/>) can be resolved safely.
/// </param>
[GuildOnly]
[SlashCommandGroup("server", "Commands regarding the server")]
public class Server(IServiceScopeFactory scopeFactory) : ApplicationCommandModule
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    /// <summary>
    /// Initializes Voyager on the current Discord server by creating or
    /// binding the Game Master role and Admin channel, configuring the
    /// server's operating <see cref="ServerMode"/>, and persisting the server record.
    /// </summary>
    /// <param name="ctx">The DSharpPlus slash command interaction context.</param>
    /// <param name="serverMode">The operating mode for the server (<see cref="ServerMode.Normal"/> or <see cref="ServerMode.Westmarch"/>).</param>
    /// <param name="createGmRole">Whether Voyager should create a new Game Master role on the server.</param>
    /// <param name="createAdminChannel">Whether Voyager should create a new private admin channel on the server.</param>
    /// <param name="GmRole">Existing GM role to use (or template when <paramref name="createGmRole"/> is set).</param>
    /// <param name="AdminChannel">Existing admin channel to use (recommended to be a private channel).</param>
    [SlashCommandPermissions(Permissions.ManageRoles | Permissions.ManageChannels)]
    [SlashCommand("setup", "Setup Voyager on your server")]
    public async Task Setup(InteractionContext ctx,
        [Option("ServerMode", "The mode of server to setup")] ServerMode serverMode,
        [Option("CreateGMRole", "Whether to create a GM role")] bool createGmRole,
        [Option("CreateAdminChannel", "Whether to create an admin channel")] bool createAdminChannel,
        [Option("GMRole", "The GM role to use")] DiscordRole? GmRole = null,
        [Option("AdminChannel", "The admin channel to use (It is recommended that it is a private channel)")] DiscordChannel? AdminChannel = null)
    {
        // Defer interaction — gives us up to 15 minutes to reply
        await ctx.DeferAsync(ephemeral: true);
        await using var scope = _scopeFactory.CreateAsyncScope();
        var handler = CreateHandler(scope.ServiceProvider, true);

        var message = await handler.SetupAsync(ctx.Guild, ctx.Guild.OwnerId == ctx.User.Id, serverMode,
            createGmRole, createAdminChannel, GmRole, AdminChannel);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder(message));
    }

    /// <summary>
    /// Displays the current server's Voyager profile and settings.
    /// </summary>
    /// <param name="ctx">The DSharpPlus slash command interaction context.</param>
    [SlashCommand("view", "View the current server settings")]
    public async Task View(InteractionContext ctx)
    {
        await ctx.DeferAsync(ephemeral: true);
        await using var scope = _scopeFactory.CreateAsyncScope();
        var handler = CreateHandler(scope.ServiceProvider, true);

        var message = await handler.ViewAsync(ctx.Guild.Id);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder(message));
    }

    /// <summary>
    /// Displays all available palette colors supported by Voyager.
    /// </summary>
    /// <param name="ctx">The DSharpPlus slash command interaction context.</param>
    [SlashCommand("colors", "View all available palette colors supported by Voyager")]
    public async Task Colors(InteractionContext ctx)
    {
        await ctx.DeferAsync(ephemeral: true);
        await using var scope = _scopeFactory.CreateAsyncScope();
        var handler = CreateHandler(scope.ServiceProvider, true);

        var message = await handler.ColorsAsync(ctx.Guild.Id);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder(message));
    }

    /// <summary>
    /// Displays a modal to update the server's settings (description and notification
    /// palette colors), optionally updating the Game Master role or admin channel.
    /// </summary>
    /// <param name="ctx">The DSharpPlus slash command interaction context.</param>
    /// <param name="gmRole">Optional new Game Master role for the server.</param>
    /// <param name="adminChannel">Optional new Voyager admin channel for the server.</param>
    [SlashCommandPermissions(Permissions.ManageRoles | Permissions.ManageChannels)]
    [SlashCommand("change", "Change the current server settings")]
    public async Task Change(InteractionContext ctx,
        [Option("GMRole", "The GM role to use")] DiscordRole? gmRole = null,
        [Option("AdminChannel", "The admin channel to use")] DiscordChannel? adminChannel = null)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var serverService = scope.ServiceProvider.GetRequiredService<IServerService>();
        var enumService = scope.ServiceProvider.GetRequiredService<IEnumService>();

        var server = await serverService.GetByIdAsync(ctx.Guild.Id);
        if (server is null)
        {
            var viewFactory = new ViewFactory(isEphemeral: true, enumService, new ServerSettings());
            var error = viewFactory.CreateNotification(NotificationType.Error, "Error: This server is not set up yet.");
            await ctx.CreateResponseAsync(
                InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder(error).AsEphemeral(true));
            return;
        }

        var handler = CreateHandler(scope.ServiceProvider, true);
        var modal = handler.CreateChangeModal(server, gmRole?.Id, adminChannel?.Id);

        await ctx.CreateResponseAsync(InteractionResponseType.Modal, modal);
    }

    /// <summary>
    /// Resolves scoped services from <paramref name="services"/> and constructs
    /// a <see cref="ServerHandler"/> configured for the current interaction's
    /// ephemerality.
    /// </summary>
    /// <param name="services">The active per-interaction scoped service provider.</param>
    /// <param name="isEphemeral">Whether the interaction response is ephemeral.</param>
    private static ServerHandler CreateHandler(IServiceProvider services, bool isEphemeral)
    {
        var serverService = services.GetRequiredService<IServerService>();
        var enumService = services.GetRequiredService<IEnumService>();

        // The command stays a thin adapter: it only translates the Discord
        // interaction into primitives and hands off to ServerHandler, which
        // holds the testable logic.
        var viewFactory = new ViewFactory(isEphemeral, enumService, new ServerSettings());
        return new ServerHandler(serverService, enumService, viewFactory);
    }
}
