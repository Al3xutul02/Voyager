using BusinessLogic.Dtos.Game;
using BusinessLogic.Enums.Types;
using BusinessLogic.Services.Abstractions;
using DSharpPlus;
using DSharpPlus.Entities;
using Voyager.API.Enums.Types;
using Voyager.API.Views;

namespace Voyager.API.Commands.Handlers;

/// <summary>
/// Testable core of the <c>/game</c> command group (<c>create</c>, <c>edit</c>,
/// <c>delete</c>, <c>details</c>, <c>invite</c>) and its button interactions
/// (confirm/cancel deletion, accept/decline DM invitation).
/// </summary>
public class GameHandler(
    IGameService gameService,
    IServerService serverService,
    IUserService userService,
    IFileService fileService,
    IEnumService enumService,
    ViewFactory viewFactory)
{
    private readonly IGameService _gameService = gameService;
    private readonly IServerService _serverService = serverService;
    private readonly IUserService _userService = userService;
    private readonly IFileService _fileService = fileService;
    private readonly IEnumService _enumService = enumService;
    private readonly ViewFactory _viewFactory = viewFactory;

    /// <summary>
    /// Evaluates whether a caller is a Game Master or Administrator on the server:
    /// returns <c>true</c> if the caller is the server owner, has <see cref="Permissions.Administrator"/>,
    /// or holds the server's configured Game Master role (<paramref name="gmRoleId"/>).
    /// </summary>
    public static bool IsGmOrAdmin(
        bool isServerOwner,
        bool isAdmin,
        ulong gmRoleId,
        IEnumerable<ulong>? callerRoleIds)
    {
        if (isServerOwner || isAdmin)
        {
            return true;
        }

        return callerRoleIds is not null && callerRoleIds.Contains(gmRoleId);
    }

    /// <summary>
    /// Evaluates whether a caller can edit or delete a specific game:
    /// returns <c>true</c> if the caller is the Game Master who created the game,
    /// the server owner, or a server administrator.
    /// </summary>
    public static bool CanManageGame(
        ulong callerId,
        ulong gameMasterId,
        bool isServerOwner,
        bool isAdmin)
    {
        return callerId == gameMasterId || isServerOwner || isAdmin;
    }

    /// <summary>
    /// Builds the response for <c>/game create</c>.
    /// Validates Normal server mode, GM/admin permissions, caller profile, 5-game server cap,
    /// unique game name on the server, and valid color. Creates the Discord category and 4 child
    /// channels (<c>gm-channel</c>, <c>general</c>, <c>characters</c>, <c>npcs</c>), downloads
    /// images to local storage, and persists the game record.
    /// </summary>
    public async Task<DiscordMessageBuilder> CreateAsync(
        DiscordGuild guild,
        ulong callerId,
        bool isServerOwner,
        bool isAdmin,
        IEnumerable<ulong>? callerRoleIds,
        string name,
        string description,
        string color,
        string gmTokenUrl,
        string gmTokenFileName,
        string thumbnailUrl,
        string thumbnailFileName)
    {
        var server = await _serverService.GetByIdAsync(guild.Id);
        if (server is null)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: This server is not set up yet.");
        }

        _viewFactory.Settings = server.Settings;

        if (server.Settings.Mode != ServerMode.Normal)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: Game commands are only available on servers configured in Normal mode.");
        }

        if (!IsGmOrAdmin(isServerOwner, isAdmin, server.GMRoleId, callerRoleIds))
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: Only Game Masters or server administrators can create games.");
        }

        var user = await _userService.GetByIdAsync(callerId);
        if (user is null)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: You need to create a profile first before creating a game. Use /profile create.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: Game name cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: Game description cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(color) || !Enum.TryParse<Color>(color.Trim(), ignoreCase: true, out _))
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                $"Error: '{color}' is not a valid color name.",
                $"**Available Colors:**\n{ServerHandler.AllAvailableColors}");
        }

        var trimmedName = name.Trim();
        var currentCount = await _gameService.GetCountByServerIdAsync(guild.Id);
        if (currentCount >= IGameService.MaxGamesPerServer)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                $"Error: This server has reached the maximum limit of {IGameService.MaxGamesPerServer} active games.");
        }

        var existing = await _gameService.GetByServerAndNameAsync(guild.Id, trimmedName);
        if (existing is not null)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Warning,
                $"Warning: A game named '{trimmedName}' already exists on this server.");
        }

        var paletteColor = _enumService.ConvertColor(color);

        try
        {
            var category = await guild.CreateChannelCategoryAsync(trimmedName, reason: $"Voyager game '{trimmedName}' category creation");

            var gmChannel = await guild.CreateChannelAsync("gm-channel", ChannelType.Text, parent: category, reason: $"Voyager game '{trimmedName}' gm-channel");
            var generalChannel = await guild.CreateChannelAsync("general", ChannelType.Text, parent: category, reason: $"Voyager game '{trimmedName}' general channel");
            var charactersChannel = await guild.CreateChannelAsync("characters", ChannelType.Text, parent: category, reason: $"Voyager game '{trimmedName}' characters channel");
            var npcsChannel = await guild.CreateChannelAsync("npcs", ChannelType.Text, parent: category, reason: $"Voyager game '{trimmedName}' npcs channel");

            // Configure gm-channel and npcs as private to GM role + bot (admins/owner always have access)
            var gmRole = guild.GetRole(server.GMRoleId);
            foreach (var privateChannel in new[] { gmChannel, npcsChannel })
            {
                await privateChannel.AddOverwriteAsync(
                    guild.CurrentMember,
                    allow: Permissions.AccessChannels | Permissions.SendMessages | Permissions.ManageChannels,
                    deny: Permissions.None);

                if (gmRole is not null)
                {
                    await privateChannel.AddOverwriteAsync(
                        gmRole,
                        allow: Permissions.AccessChannels | Permissions.SendMessages,
                        deny: Permissions.None);
                }

                await privateChannel.AddOverwriteAsync(
                    guild.EveryoneRole,
                    allow: Permissions.None,
                    deny: Permissions.AccessChannels);
            }

            var folderName = Guid.NewGuid().ToString("N");
            var relativeDir = $"Games/{guild.Id}/{folderName}";

            var gmTokenExt = Path.GetExtension(gmTokenFileName);
            if (string.IsNullOrWhiteSpace(gmTokenExt)) gmTokenExt = ".png";
            var thumbExt = Path.GetExtension(thumbnailFileName);
            if (string.IsNullOrWhiteSpace(thumbExt)) thumbExt = ".png";

            var gmTokenPath = await _fileService.SaveAttachmentAsync(gmTokenUrl, relativeDir, $"gm_token{gmTokenExt}");
            var thumbnailPath = await _fileService.SaveAttachmentAsync(thumbnailUrl, relativeDir, $"thumbnail{thumbExt}");

            var createDto = new GameCreateDto(
                ServerId: guild.Id,
                GameMasterId: callerId,
                Name: trimmedName,
                Description: description.Trim(),
                Color: paletteColor,
                GmTokenImagePath: gmTokenPath,
                ThumbnailImagePath: thumbnailPath,
                CategoryId: category.Id,
                GmChannelId: gmChannel.Id,
                GeneralChannelId: generalChannel.Id,
                CharactersChannelId: charactersChannel.Id,
                NpcsChannelId: npcsChannel.Id);

            await _gameService.CreateAsync(createDto);

            return _viewFactory.CreateNotification(
                NotificationType.Success,
                $"Game '{trimmedName}' created successfully.");
        }
        catch (Exception)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: Something went wrong while creating the game and its channels.");
        }
    }

    /// <summary>
    /// Builds the response for <c>/game edit</c>.
    /// Validates Normal server mode, GM/admin permissions, and that the caller is either the
    /// game's creator GM or a server admin/owner. Updates any provided optional fields and
    /// renames the Discord channel category if the game name changed.
    /// </summary>
    public async Task<DiscordMessageBuilder> EditAsync(
        DiscordGuild guild,
        ulong callerId,
        bool isServerOwner,
        bool isAdmin,
        IEnumerable<ulong>? callerRoleIds,
        string name,
        string? newName = null,
        string? description = null,
        string? color = null,
        string? gmTokenUrl = null,
        string? gmTokenFileName = null,
        string? thumbnailUrl = null,
        string? thumbnailFileName = null)
    {
        var server = await _serverService.GetByIdAsync(guild.Id);
        if (server is null)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: This server is not set up yet.");
        }

        _viewFactory.Settings = server.Settings;

        if (server.Settings.Mode != ServerMode.Normal)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: Game commands are only available on servers configured in Normal mode.");
        }

        if (!IsGmOrAdmin(isServerOwner, isAdmin, server.GMRoleId, callerRoleIds))
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: Only Game Masters or server administrators can edit games.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: Game name cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(newName) &&
            string.IsNullOrWhiteSpace(description) &&
            string.IsNullOrWhiteSpace(color) &&
            string.IsNullOrWhiteSpace(gmTokenUrl) &&
            string.IsNullOrWhiteSpace(thumbnailUrl))
        {
            return _viewFactory.CreateNotification(
                NotificationType.Warning,
                "Warning: No fields were provided to update.");
        }

        var game = await _gameService.GetByServerAndNameAsync(guild.Id, name.Trim());
        if (game is null)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                $"Error: Game '{name.Trim()}' was not found on this server.");
        }

        if (!CanManageGame(callerId, game.GameMasterId, isServerOwner, isAdmin))
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: You can only edit games that you created yourself, unless you are a server administrator.");
        }

        if (!string.IsNullOrWhiteSpace(color) && !Enum.TryParse<Color>(color.Trim(), ignoreCase: true, out _))
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                $"Error: '{color}' is not a valid color name.",
                $"**Available Colors:**\n{ServerHandler.AllAvailableColors}");
        }

        var updatedName = game.Name;
        bool isRenamed = false;
        if (!string.IsNullOrWhiteSpace(newName))
        {
            var trimmedNewName = newName.Trim();
            if (!string.Equals(trimmedNewName, game.Name, StringComparison.OrdinalIgnoreCase))
            {
                var duplicate = await _gameService.GetByServerAndNameAsync(guild.Id, trimmedNewName);
                if (duplicate is not null && duplicate.Id != game.Id)
                {
                    return _viewFactory.CreateNotification(
                        NotificationType.Warning,
                        $"Warning: A game named '{trimmedNewName}' already exists on this server.");
                }
            }

            if (!string.Equals(trimmedNewName, game.Name, StringComparison.Ordinal))
            {
                updatedName = trimmedNewName;
                isRenamed = true;
            }
        }

        try
        {
            if (isRenamed)
            {
                var category = guild.GetChannel(game.CategoryId);
                if (category is not null)
                {
                    await category.ModifyAsync(m => m.Name = updatedName);
                }
            }

            var gmTokenPath = game.GmTokenImagePath;
            var thumbnailPath = game.ThumbnailImagePath;
            var dir = Path.GetDirectoryName(game.ThumbnailImagePath)?.Replace('\\', '/')
                ?? $"Games/{guild.Id}/{Guid.NewGuid():N}";

            if (!string.IsNullOrWhiteSpace(gmTokenUrl) && !string.IsNullOrWhiteSpace(gmTokenFileName))
            {
                var ext = Path.GetExtension(gmTokenFileName);
                if (string.IsNullOrWhiteSpace(ext)) ext = ".png";
                gmTokenPath = await _fileService.SaveAttachmentAsync(gmTokenUrl, dir, $"gm_token{ext}");
                if (gmTokenPath != game.GmTokenImagePath && !string.IsNullOrWhiteSpace(game.GmTokenImagePath))
                {
                    _fileService.DeleteFile(game.GmTokenImagePath);
                }
            }

            if (!string.IsNullOrWhiteSpace(thumbnailUrl) && !string.IsNullOrWhiteSpace(thumbnailFileName))
            {
                var ext = Path.GetExtension(thumbnailFileName);
                if (string.IsNullOrWhiteSpace(ext)) ext = ".png";
                thumbnailPath = await _fileService.SaveAttachmentAsync(thumbnailUrl, dir, $"thumbnail{ext}");
                if (thumbnailPath != game.ThumbnailImagePath && !string.IsNullOrWhiteSpace(game.ThumbnailImagePath))
                {
                    _fileService.DeleteFile(game.ThumbnailImagePath);
                }
            }

            var updatedColor = !string.IsNullOrWhiteSpace(color)
                ? _enumService.ConvertColor(color)
                : game.Color;

            var updatedDescription = !string.IsNullOrWhiteSpace(description)
                ? description.Trim()
                : game.Description;

            var updateDto = new GameUpdateDto(
                Id: game.Id,
                ServerId: game.ServerId,
                GameMasterId: game.GameMasterId,
                Name: updatedName,
                Description: updatedDescription,
                Color: updatedColor,
                GmTokenImagePath: gmTokenPath,
                ThumbnailImagePath: thumbnailPath,
                CategoryId: game.CategoryId,
                GmChannelId: game.GmChannelId,
                GeneralChannelId: game.GeneralChannelId,
                CharactersChannelId: game.CharactersChannelId,
                NpcsChannelId: game.NpcsChannelId);

            await _gameService.UpdateAsync(updateDto);

            return _viewFactory.CreateNotification(
                NotificationType.Success,
                $"Game '{updatedName}' updated successfully.");
        }
        catch (Exception)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: Something went wrong while updating the game.");
        }
    }

    /// <summary>
    /// Builds the response for <c>/game delete</c>.
    /// Validates Normal server mode, GM/admin permissions, and that the caller is either the
    /// game's creator GM or a server admin/owner, then returns the warning confirmation prompt.
    /// </summary>
    public async Task<DiscordMessageBuilder> DeletePromptAsync(
        ulong guildId,
        ulong callerId,
        bool isServerOwner,
        bool isAdmin,
        IEnumerable<ulong>? callerRoleIds,
        string name)
    {
        var server = await _serverService.GetByIdAsync(guildId);
        if (server is null)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: This server is not set up yet.");
        }

        _viewFactory.Settings = server.Settings;

        if (server.Settings.Mode != ServerMode.Normal)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: Game commands are only available on servers configured in Normal mode.");
        }

        if (!IsGmOrAdmin(isServerOwner, isAdmin, server.GMRoleId, callerRoleIds))
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: Only Game Masters or server administrators can delete games.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: Game name cannot be empty.");
        }

        var game = await _gameService.GetByServerAndNameAsync(guildId, name.Trim());
        if (game is null)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                $"Error: Game '{name.Trim()}' was not found on this server.");
        }

        if (!CanManageGame(callerId, game.GameMasterId, isServerOwner, isAdmin))
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: You can only delete games that you created yourself, unless you are a server administrator.");
        }

        return _viewFactory.CreateGameDeleteConfirmation(game, callerId);
    }

    /// <summary>
    /// Executes game deletion after the authorized caller clicks the "Confirm Delete" button:
    /// archives the game's Discord channels (renames category to <c>[Archived] {Name}</c> and makes
    /// all game channels read-only), soft-deletes the game record (which also clears <c>CurrentGame</c>
    /// on associated characters), and deletes the stored image directory.
    /// </summary>
    public async Task<DiscordMessageBuilder> ConfirmDeleteAsync(
        DiscordGuild? guild,
        ulong callerId,
        ulong gameId,
        ulong authorizedUserId)
    {
        if (callerId != authorizedUserId)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: You are not authorized to confirm this game deletion.");
        }

        var game = await _gameService.GetByIdAsync(gameId);
        if (game is null)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: Game not found or already deleted.");
        }

        var server = await _serverService.GetByIdAsync(game.ServerId);
        if (server is not null)
        {
            _viewFactory.Settings = server.Settings;
        }

        try
        {
            if (guild is not null)
            {
                await ArchiveGameChannelsAsync(guild, game, server?.GMRoleId);
            }

            await _gameService.DeleteAsync(gameId);

            if (!string.IsNullOrWhiteSpace(game.ThumbnailImagePath))
            {
                var dir = Path.GetDirectoryName(game.ThumbnailImagePath)?.Replace('\\', '/');
                if (!string.IsNullOrWhiteSpace(dir))
                {
                    _fileService.DeleteDirectory(dir);
                }
            }

            return _viewFactory.CreateNotification(
                NotificationType.Success,
                $"Game '{game.Name}' was deleted and its channels have been archived.");
        }
        catch (Exception)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: Something went wrong while deleting the game.");
        }
    }

    private static async Task ArchiveGameChannelsAsync(DiscordGuild guild, GameReadDto game, ulong? gmRoleId)
    {
        var category = guild.GetChannel(game.CategoryId);
        if (category is not null)
        {
            var archivedName = $"[Archived] {game.Name}";
            if (archivedName.Length > 100)
            {
                archivedName = archivedName[..100];
            }

            await category.ModifyAsync(m => m.Name = archivedName);
        }

        var gmRole = gmRoleId.HasValue ? guild.GetRole(gmRoleId.Value) : null;
        var channelIds = new[]
        {
            game.GmChannelId,
            game.GeneralChannelId,
            game.CharactersChannelId,
            game.NpcsChannelId
        };

        foreach (var channelId in channelIds)
        {
            var channel = guild.GetChannel(channelId);
            if (channel is null)
            {
                continue;
            }

            bool isPrivateGmChannel = channelId == game.GmChannelId || channelId == game.NpcsChannelId;
            if (isPrivateGmChannel)
            {
                await channel.AddOverwriteAsync(
                    guild.EveryoneRole,
                    allow: Permissions.None,
                    deny: Permissions.AccessChannels | Permissions.SendMessages);

                if (gmRole is not null)
                {
                    await channel.AddOverwriteAsync(
                        gmRole,
                        allow: Permissions.AccessChannels,
                        deny: Permissions.SendMessages);
                }
            }
            else
            {
                await channel.AddOverwriteAsync(
                    guild.EveryoneRole,
                    allow: Permissions.None,
                    deny: Permissions.SendMessages);

                if (gmRole is not null)
                {
                    await channel.AddOverwriteAsync(
                        gmRole,
                        allow: Permissions.None,
                        deny: Permissions.SendMessages);
                }
            }
        }
    }

    /// <summary>
    /// Builds the response for <c>/game details</c>.
    /// If <paramref name="name"/> is provided, looks up the game by name on the server.
    /// If <paramref name="name"/> is omitted, looks up the game that <paramref name="channelId"/>
    /// (or <paramref name="parentChannelId"/>) belongs to.
    /// </summary>
    public async Task<DiscordMessageBuilder> DetailsAsync(
        ulong guildId,
        ulong channelId,
        ulong? parentChannelId = null,
        string? name = null)
    {
        var server = await _serverService.GetByIdAsync(guildId);
        if (server is null)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: This server is not set up yet.");
        }

        _viewFactory.Settings = server.Settings;

        if (server.Settings.Mode != ServerMode.Normal)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: Game commands are only available on servers configured in Normal mode.");
        }

        GameReadDto? game;
        if (!string.IsNullOrWhiteSpace(name))
        {
            game = await _gameService.GetByServerAndNameAsync(guildId, name.Trim());
            if (game is null)
            {
                return _viewFactory.CreateNotification(
                    NotificationType.Error,
                    $"Error: Game '{name.Trim()}' was not found on this server.");
            }
        }
        else
        {
            game = await _gameService.GetByChannelIdAsync(guildId, channelId);
            if (game is null && parentChannelId.HasValue)
            {
                game = await _gameService.GetByChannelIdAsync(guildId, parentChannelId.Value);
            }

            if (game is null)
            {
                return _viewFactory.CreateNotification(
                    NotificationType.Error,
                    "Error: This channel is not part of any active game. Please provide a game name or run this command inside a game channel.");
            }
        }

        Stream? thumbnailStream = _fileService.OpenRead(game.ThumbnailImagePath);
        return _viewFactory.CreateGameDetails(game, thumbnailStream);
    }

    /// <summary>
    /// Builds the response for <c>/game invite</c>.
    /// Enforces <c>[GM Channel]</c> rules: must be executed inside the game's <c>gm-channel</c>
    /// by the game's Game Master. Validates the invited member has a profile and is not already
    /// in the game, sends the DM invite via <paramref name="sendDmAsync"/>, and returns an
    /// ephemeral confirmation notification.
    /// </summary>
    public async Task<DiscordMessageBuilder> InviteAsync(
        ulong guildId,
        ulong channelId,
        ulong callerId,
        ulong targetUserId,
        string targetUsername,
        Func<DiscordMessageBuilder, Task> sendDmAsync)
    {
        var server = await _serverService.GetByIdAsync(guildId);
        if (server is null)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: This server is not set up yet.");
        }

        _viewFactory.Settings = server.Settings;

        if (server.Settings.Mode != ServerMode.Normal)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: Game commands are only available on servers configured in Normal mode.");
        }

        var game = await _gameService.GetByChannelIdAsync(guildId, channelId);
        if (game is null || game.GmChannelId != channelId)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: /game invite can only be used inside a game's gm-channel.");
        }

        if (game.GameMasterId != callerId)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: Only the Game Master of this game can invite players.");
        }

        if (targetUserId == callerId)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Warning,
                "Warning: You are already the Game Master of this game.");
        }

        var targetUser = await _userService.GetByIdAsync(targetUserId);
        if (targetUser is null)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                $"Error: User '{targetUsername}' does not have a Voyager profile yet. They must run /profile create first.");
        }

        if (await _gameService.IsPlayerInGameAsync(game.Id, targetUserId))
        {
            return _viewFactory.CreateNotification(
                NotificationType.Warning,
                $"Warning: '{targetUsername}' is already a player in '{game.Name}'.");
        }

        try
        {
            await using Stream? thumbnailStream = _fileService.OpenRead(game.ThumbnailImagePath);
            var dmInviteMessage = _viewFactory.CreateGameInvite(game, server.Name, targetUserId, thumbnailStream);
            await sendDmAsync(dmInviteMessage);

            return _viewFactory.CreateNotification(
                NotificationType.Success,
                $"Invitation to join '{game.Name}' has been sent to {targetUsername}.");
        }
        catch (Exception)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                $"Error: Could not send a direct message to {targetUsername}. Please check if they have DMs enabled.");
        }
    }

    /// <summary>
    /// Handles a player clicking "Accept" or "Deny" on a game invitation in their DMs.
    /// If accepted, enrolls the player in the game. Returns both the updated DM message
    /// and the notification message to post to the game's <c>gm-channel</c> (along with <c>GmChannelId</c>).
    /// </summary>
    public async Task<(DiscordMessageBuilder DmResponse, ulong? GmChannelId, DiscordMessageBuilder? GmChannelNotification)> HandleInviteResponseAsync(
        ulong callerId,
        string callerUsername,
        ulong gameId,
        ulong invitedUserId,
        bool accepted)
    {
        if (callerId != invitedUserId)
        {
            var unauthorized = _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: This game invitation is not for you.");
            return (unauthorized, null, null);
        }

        var game = await _gameService.GetByIdAsync(gameId);
        if (game is null)
        {
            var notFound = _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: This game no longer exists.");
            return (notFound, null, null);
        }

        var server = await _serverService.GetByIdAsync(game.ServerId);
        if (server is not null)
        {
            _viewFactory.Settings = server.Settings;
        }

        var user = await _userService.GetByIdAsync(callerId);
        if (user is null)
        {
            var noProfile = _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: You need a Voyager profile before joining a game. Use /profile create.");
            return (noProfile, null, null);
        }

        var displayName = !string.IsNullOrWhiteSpace(user.Name) ? user.Name : callerUsername;
        var gmViewFactory = new ViewFactory(isEphemeral: false, _enumService, _viewFactory.Settings);

        if (accepted)
        {
            if (await _gameService.IsPlayerInGameAsync(game.Id, callerId))
            {
                var alreadyJoined = _viewFactory.CreateNotification(
                    NotificationType.Warning,
                    $"You are already a player in '{game.Name}'.");
                return (alreadyJoined, null, null);
            }

            await _gameService.AddPlayerAsync(game.Id, callerId);

            var dmAccepted = _viewFactory.CreateNotification(
                NotificationType.Success,
                $"You accepted the invitation to join '{game.Name}'!");

            var gmAccepted = gmViewFactory.CreateNotification(
                NotificationType.Success,
                $"Player Invitation Accepted: {displayName}",
                $"**{displayName}** (<@{callerId}>) has accepted the invitation and joined **{game.Name}**.");

            return (dmAccepted, game.GmChannelId, gmAccepted);
        }
        else
        {
            var dmDeclined = _viewFactory.CreateNotification(
                NotificationType.Warning,
                $"You declined the invitation to join '{game.Name}'.");

            var gmDeclined = gmViewFactory.CreateNotification(
                NotificationType.Warning,
                $"Player Invitation Declined: {displayName}",
                $"**{displayName}** (<@{callerId}>) has declined the invitation to join **{game.Name}**.");

            return (dmDeclined, game.GmChannelId, gmDeclined);
        }
    }
}
