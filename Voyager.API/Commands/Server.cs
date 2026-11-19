using BusinessLogic.Enums.Types;
using BusinessLogic.Json.Models;
using BusinessLogic.Services.Abstractions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Voyager.API.Commands.Autocompletes;
using Voyager.API.Commands.Handlers;
using Voyager.API.Views;

namespace Voyager.API.Commands;

[GuildOnly]
[SlashCommandGroup("server", "Commands regarding the server")]
public class Server(IServiceScopeFactory scopeFactory) : ApplicationCommandModule
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    [SlashCommandPermissions(Permissions.ManageRoles | Permissions.ManageChannels)]
    [SlashCommand("setup", "Setup Voyager on your server")]
    public async Task Setup(InteractionContext ctx,
        [Option("Server Mode", "The mode of server to setup")] ServerMode serverMode,
        [Option("Create GM Role", "Whether to create a GM role")]  bool createGmRole,
        [Option("Create Admin Channel", "Whether to create an admin channel")] bool createAdminChannel,
        [Option("GM Role", "The GM role to use")] DiscordRole? GmRole,
        [Option("Admin Channel", "The admin channel to use (It is recommended that it is a private channel)")] DiscordChannel? AdminChannel)
    {
        // Defer interaction — gives us up to 15 minutes to reply
        await ctx.DeferAsync(ephemeral: true);
        await using var scope = _scopeFactory.CreateAsyncScope();
        var handler = CreateHandler(scope.ServiceProvider, true);
        
        var message = await handler.SetupAsync(ctx.Guild, ctx.Guild.OwnerId == ctx.User.Id, serverMode,
            createGmRole, createAdminChannel, GmRole, AdminChannel);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder(message));
    }

    [SlashCommand("view", "View the current server settings")]
    public async Task View(InteractionContext ctx)
    {
        await ctx.DeferAsync(ephemeral: true);
        await using var scope = _scopeFactory.CreateAsyncScope();
        var handler = CreateHandler(scope.ServiceProvider, true);

        var message = await handler.ViewAsync(ctx.Guild.Id);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder(message));
    }

    [SlashCommand("change", "Change the current server settings")]
    public async Task Change(InteractionContext ctx,
        [Option("GM Role", "The GM role to use")] DiscordRole? gmRole,
        [Option("Admin Channel", "The admin channel to use")] DiscordChannel? adminChannel,
        [Option("Description", "The description for the server")] string? description,
        [Autocomplete(typeof(ColorAutocompleteProvider))]
        [Option("General Color", "The general color for the server", true)] string? generalColor,
        [Autocomplete(typeof(ColorAutocompleteProvider))]
        [Option("Error Color", "The error color for the server", true)] string? errorColor,
        [Autocomplete(typeof(ColorAutocompleteProvider))]
        [Option("Success Color", "The success color for the server", true)] string? successColor,
        [Autocomplete(typeof(ColorAutocompleteProvider))]
        [Option("Warning Color", "The warning color for the server", true)] string? warningColor)
    {
        await ctx.DeferAsync(ephemeral: true);
        await using var scope = _scopeFactory.CreateAsyncScope();
        var handler = CreateHandler(scope.ServiceProvider, true);

        var message = await handler.ChangeAsync(ctx.Guild.Id,
            gmRole, adminChannel, description, generalColor, errorColor, successColor, warningColor);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder(message));
    }

    private static ServerHandler CreateHandler(IServiceProvider services, bool isEphemeral)
    {
        var serverService = services.GetRequiredService<IServerService>();
        var enumService = services.GetRequiredService<IEnumService>();

        // The command stays a thin adapter: it only translates the Discord
        // interaction into primitives and hands off to ProfileHandler, which
        // holds the testable logic.
        var viewFactory = new ViewFactory(isEphemeral, enumService, new ServerSettings());
        return new ServerHandler(serverService, enumService, viewFactory);
    }
}
