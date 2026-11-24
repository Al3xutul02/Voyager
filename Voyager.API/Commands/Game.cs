using BusinessLogic.Json.Models;
using BusinessLogic.Services.Abstractions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Voyager.API.Commands.Autocompletes;
using Voyager.API.Commands.Handlers;
using Voyager.API.Views;

namespace Voyager.API.Commands;

/// <summary>
/// Slash command group for creating, editing, deleting, viewing, and inviting players to games
/// (<c>/game create</c>, <c>/game edit</c>, <c>/game delete</c>, <c>/game details</c>, <c>/game invite</c>).
/// Restricted to guild channels via <see cref="GuildOnlyAttribute"/>.
/// </summary>
/// <param name="scopeFactory">Factory used to resolve scoped services per interaction.</param>
[GuildOnly]
[SlashCommandGroup("game", "Commands for creating and managing games")]
public class Game(IServiceScopeFactory scopeFactory) : ApplicationCommandModule
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    /// <summary>
    /// Creates a new game on a server in Normal mode along with its Discord category and channels
    /// (<c>gm-channel</c>, <c>general</c>, <c>characters</c>, <c>npcs</c>).
    /// </summary>
    [SlashCommand("create", "Create a new game with its channel category and game channels")]
    public async Task Create(
        InteractionContext ctx,
        [Option("Name", "Name of the game")] string name,
        [Option("Description", "Description or synopsis of the game")] string description,
        [Autocomplete(typeof(ColorAutocompleteProvider))]
        [Option("Color", "Palette color for the game's embeds", true)] string color,
        [Option("GMTokenImage", "GM Token image for the game")] DiscordAttachment gmTokenImage,
        [Option("ThumbnailImage", "Thumbnail image for the game")] DiscordAttachment thumbnailImage)
    {
        await ctx.DeferAsync(ephemeral: true);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var handler = await CreateHandlerAsync(scope.ServiceProvider, ctx.Guild.Id, isEphemeral: true);
        var (isOwner, isAdmin, roleIds) = ExtractCallerPermissions(ctx);

        var message = await handler.CreateAsync(
            ctx.Guild,
            ctx.User.Id,
            isOwner,
            isAdmin,
            roleIds,
            name,
            description,
            color,
            gmTokenImage.Url,
            gmTokenImage.FileName,
            thumbnailImage.Url,
            thumbnailImage.FileName);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder(message));
    }

    /// <summary>
    /// Edits information about an existing game (name, description, color, GM token image, thumbnail image).
    /// Updates the game's Discord channel category name if the name is changed.
    /// </summary>
    [SlashCommand("edit", "Edit information about an existing game")]
    public async Task Edit(
        InteractionContext ctx,
        [Autocomplete(typeof(GameAutocompleteProvider))]
        [Option("Name", "Name of the game you want to edit", true)] string name,
        [Option("NewName", "Optional new name for the game")] string? newName = null,
        [Option("Description", "Optional new description for the game")] string? description = null,
        [Autocomplete(typeof(ColorAutocompleteProvider))]
        [Option("Color", "Optional new palette color for the game", true)] string? color = null,
        [Option("GMTokenImage", "Optional new GM Token image")] DiscordAttachment? gmTokenImage = null,
        [Option("ThumbnailImage", "Optional new thumbnail image")] DiscordAttachment? thumbnailImage = null)
    {
        await ctx.DeferAsync(ephemeral: true);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var handler = await CreateHandlerAsync(scope.ServiceProvider, ctx.Guild.Id, isEphemeral: true);
        var (isOwner, isAdmin, roleIds) = ExtractCallerPermissions(ctx);

        var message = await handler.EditAsync(
            ctx.Guild,
            ctx.User.Id,
            isOwner,
            isAdmin,
            roleIds,
            name,
            newName,
            description,
            color,
            gmTokenImage?.Url,
            gmTokenImage?.FileName,
            thumbnailImage?.Url,
            thumbnailImage?.FileName);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder(message));
    }

    /// <summary>
    /// Prompts the caller with an irreversible deletion warning and Confirm/Cancel buttons.
    /// </summary>
    [SlashCommand("delete", "Delete a game and archive its channels")]
    public async Task Delete(
        InteractionContext ctx,
        [Autocomplete(typeof(GameAutocompleteProvider))]
        [Option("Name", "Name of the game you want to delete", true)] string name)
    {
        await ctx.DeferAsync(ephemeral: false);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var handler = await CreateHandlerAsync(scope.ServiceProvider, ctx.Guild.Id, isEphemeral: false);
        var (isOwner, isAdmin, roleIds) = ExtractCallerPermissions(ctx);

        var message = await handler.DeletePromptAsync(
            ctx.Guild.Id,
            ctx.User.Id,
            isOwner,
            isAdmin,
            roleIds,
            name);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder(message));
    }

    /// <summary>
    /// Displays an embed with information about a game (by name, or inferred from the current channel).
    /// </summary>
    [SlashCommand("details", "View information and player roster for a game")]
    public async Task Details(
        InteractionContext ctx,
        [Autocomplete(typeof(GameAutocompleteProvider))]
        [Option("Name", "Optional name of the game to view (defaults to the current channel's game)", true)] string? name = null)
    {
        await ctx.DeferAsync(ephemeral: true);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var handler = await CreateHandlerAsync(scope.ServiceProvider, ctx.Guild.Id, isEphemeral: true);

        var message = await handler.DetailsAsync(
            ctx.Guild.Id,
            ctx.Channel.Id,
            ctx.Channel.ParentId,
            name);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder(message));
    }

    /// <summary>
    /// Invites a Discord member to the game corresponding to the current <c>gm-channel</c>.
    /// Sends the player a private DM with Accept/Deny buttons and returns an ephemeral confirmation.
    /// </summary>
    [SlashCommand("invite", "Invite a player to this game (must be used in the game's gm-channel)")]
    public async Task Invite(
        InteractionContext ctx,
        [Option("Player", "The Discord member you want to invite")] DiscordUser player)
    {
        await ctx.DeferAsync(ephemeral: true);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var handler = await CreateHandlerAsync(scope.ServiceProvider, ctx.Guild.Id, isEphemeral: true);

        var message = await handler.InviteAsync(
            ctx.Guild.Id,
            ctx.Channel.Id,
            ctx.User.Id,
            player.Id,
            player.Username,
            async dmMessage =>
            {
                DiscordMember member = player is DiscordMember m
                    ? m
                    : await ctx.Guild.GetMemberAsync(player.Id);
                await member.SendMessageAsync(dmMessage);
            });

        await ctx.EditResponseAsync(new DiscordWebhookBuilder(message));
    }

    private static (bool IsOwner, bool IsAdmin, IEnumerable<ulong>? RoleIds) ExtractCallerPermissions(InteractionContext ctx)
    {
        bool isOwner = ctx.Guild.OwnerId == ctx.User.Id;
        bool isAdmin = false;
        IEnumerable<ulong>? roleIds = null;

        if (ctx.Member is not null)
        {
            isAdmin = (ctx.Member.Permissions & Permissions.Administrator) != 0;
            roleIds = ctx.Member.Roles.Select(r => r.Id);
        }

        return (isOwner, isAdmin, roleIds);
    }

    private static async Task<GameHandler> CreateHandlerAsync(
        IServiceProvider services,
        ulong guildId,
        bool isEphemeral)
    {
        var gameService = services.GetRequiredService<IGameService>();
        var serverService = services.GetRequiredService<IServerService>();
        var userService = services.GetRequiredService<IUserService>();
        var fileService = services.GetRequiredService<IFileService>();
        var enumService = services.GetRequiredService<IEnumService>();

        ServerSettings settings = new();
        var server = await serverService.GetByIdAsync(guildId);
        if (server is not null)
        {
            settings = server.Settings;
        }

        var viewFactory = new ViewFactory(isEphemeral, enumService, settings);
        return new GameHandler(gameService, serverService, userService, fileService, enumService, viewFactory);
    }
}
