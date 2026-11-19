using BusinessLogic.Dtos.Server;
using BusinessLogic.Enums.Types;
using BusinessLogic.Json.Models;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;
using Voyager.API.Enums.Types;
using Voyager.API.Views;

namespace Voyager.API.Commands.Handlers;

/// <summary>
/// Testable core of the <c>/server</c> command group (<c>setup</c>, <c>view</c>,
/// <c>change</c>). Encapsulates validation, database persistence via
/// <see cref="IServerService"/>, and response message construction via
/// <see cref="ViewFactory"/>.
/// </summary>
/// <param name="serverService">Source of server configuration data.</param>
/// <param name="enumService">Converts color names and palette enums.</param>
/// <param name="viewFactory">Builds the Discord message for the response.</param>
public class ServerHandler(IServerService serverService, IEnumService enumService, ViewFactory viewFactory)
{
    private readonly IServerService _serverService = serverService;
    private readonly IEnumService _enumService = enumService;
    private readonly ViewFactory _viewFactory = viewFactory;

    /// <summary>
    /// Builds the response for <c>/server setup</c>: validates ownership and
    /// role/channel parameters, verifies the server is not already registered,
    /// optionally creates the GM role and admin channel on Discord, persists
    /// the new server record, posts the server profile card in the admin
    /// channel, and returns a status notification.
    /// </summary>
    /// <param name="guild">The Discord guild being set up.</param>
    /// <param name="isServerOwner">Whether the command caller is the guild owner.</param>
    /// <param name="serverMode">The operating mode chosen for the server.</param>
    /// <param name="createGmRole">Whether to create a new GM role on the guild.</param>
    /// <param name="createAdminChannel">Whether to create a new admin channel on the guild.</param>
    /// <param name="gmRole">Existing GM role provided by the caller, if any.</param>
    /// <param name="adminChannel">Existing admin channel provided by the caller, if any.</param>
    public async Task<DiscordMessageBuilder> SetupAsync(
        DiscordGuild guild,
        bool isServerOwner,
        ServerMode serverMode,
        bool createGmRole,
        bool createAdminChannel,
        DiscordRole? gmRole,
        DiscordChannel? adminChannel)
    {
        if (!isServerOwner)
        {
            return _viewFactory.CreateNotification(NotificationType.Error, "Error: Only the server owner can set up Voyager.");
        }

        if (!createGmRole && gmRole is null)
        {
            return _viewFactory.CreateNotification(NotificationType.Error, "Error: GM role creation not requested and no existing role provided.");
        }

        if (!createAdminChannel && adminChannel is null)
        {
            return _viewFactory.CreateNotification(NotificationType.Error, "Error: Admin channel creation not requested and no existing channel provided.");
        }

        var result = await _serverService.GetByIdAsync(guild.Id);
        if (result is not null)
        {
            return _viewFactory.CreateNotification(NotificationType.Warning, "Error: This server is already set up.");
        }
        try
        {
            if (createGmRole)
            {
                gmRole = await guild.CreateRoleAsync("Game Master",
                    color: _enumService.ConvertColor(new ServerSettings().GeneralColor), reason: "Voyager GM role creation");
            }

            if (createAdminChannel)
            {
                adminChannel = await guild.CreateChannelAsync("voyager-admin", DSharpPlus.ChannelType.Text, reason: "Voyager admin channel creation");
            }

            // Grant the bot explicit access first before denying @everyone,
            // so the bot doesn't lock itself out of the channel when it isn't
            // a full server Administrator. (The server owner automatically
            // has full access to all channels in Discord.)
            await adminChannel!.AddOverwriteAsync(
                guild.CurrentMember,
                allow: DSharpPlus.Permissions.AccessChannels | DSharpPlus.Permissions.SendMessages,
                deny: DSharpPlus.Permissions.None);
            await adminChannel!.AddOverwriteAsync(
                guild.EveryoneRole,
                allow: DSharpPlus.Permissions.None,
                deny: DSharpPlus.Permissions.AccessChannels);

            var newServer = new ServerCreateDto(
                Id: guild.Id,
                Name: guild.Name,
                GMRoleId: gmRole!.Id,
                AdminChannelId: adminChannel!.Id,
                Settings: new ServerSettings(serverMode)
            );

            await _serverService.CreateAsync(newServer);
            var serverReadDto = await _serverService.GetByIdAsync(newServer.Id);
            await adminChannel!.SendMessageAsync(_viewFactory.CreateServerProfile(serverReadDto!));
            return _viewFactory.CreateNotification(NotificationType.Success, "Success: Voyager has been set up for this server.");
        }
        catch (Exception)
        {
            return _viewFactory.CreateNotification(NotificationType.Error, "Error: Something went wrong while setting up Voyager.");
        }
    }

    /// <summary>
    /// Builds the response for <c>/server view</c>: the server's profile card
    /// if the guild is registered, otherwise an error notification.
    /// </summary>
    /// <param name="guildId">The Discord snowflake ID of the guild to view.</param>
    public async Task<DiscordMessageBuilder> ViewAsync(ulong guildId)
    {
        var server = await _serverService.GetByIdAsync(guildId);
        if (server is null)
        {
            return _viewFactory.CreateNotification(NotificationType.Error, "Error: This server is not set up yet.");
        }
        return _viewFactory.CreateServerProfile(server);
    }

    /// <summary>
    /// Builds the response for <c>/server change</c>: updates the guild's
    /// stored configuration and <see cref="ServerSettings"/> with any non-null
    /// parameters provided. Returns a warning notification without hitting the
    /// database when no options are supplied.
    /// </summary>
    /// <param name="guildId">The Discord snowflake ID of the guild to update.</param>
    /// <param name="gmRole">Optional new Game Master role.</param>
    /// <param name="adminChannel">Optional new Voyager admin channel.</param>
    /// <param name="description">Optional new description for the server profile card.</param>
    /// <param name="generalColor">Optional new general palette color name.</param>
    /// <param name="errorColor">Optional new error notification palette color name.</param>
    /// <param name="successColor">Optional new success notification palette color name.</param>
    /// <param name="warningColor">Optional new warning notification palette color name.</param>
    public async Task<DiscordMessageBuilder> ChangeAsync(
        ulong guildId,
        DiscordRole? gmRole,
        DiscordChannel? adminChannel,
        string? description,
        string? generalColor,
        string? errorColor,
        string? successColor,
        string? warningColor)
    {
        if (string.IsNullOrEmpty(description) && string.IsNullOrEmpty(generalColor) && string.IsNullOrEmpty(errorColor) && string.IsNullOrEmpty(successColor) && string.IsNullOrEmpty(warningColor) && gmRole is null && adminChannel is null)
        {
            return _viewFactory.CreateNotification(NotificationType.Warning, "Warning: No changes were provided.");
        }

        var server = await _serverService.GetByIdAsync(guildId);
        if (server is null)
        {
            return _viewFactory.CreateNotification(NotificationType.Error, "Error: This server is not set up yet.");
        }

        try
        {
            var updateDto = new ServerUpdateDto(
                Id: guildId,
                Name: server.Name,
                Description: description ?? server.Description,
                GMRoleId: gmRole?.Id ?? server.GMRoleId,
                AdminChannelId: adminChannel?.Id ?? server.AdminChannelId,
                Settings: new ServerSettings(
                    Mode: server.Settings.Mode,
                    GeneralColor: generalColor != null ? _enumService.ConvertColor(generalColor) : server.Settings.GeneralColor,
                    ErrorColor: errorColor != null ? _enumService.ConvertColor(errorColor) : server.Settings.ErrorColor,
                    SuccessColor: successColor != null ? _enumService.ConvertColor(successColor) : server.Settings.SuccessColor,
                    WarningColor: warningColor != null ? _enumService.ConvertColor(warningColor) : server.Settings.WarningColor
                )
            );

            await _serverService.UpdateAsync(updateDto);
            return _viewFactory.CreateNotification(NotificationType.Success, "Success: Server settings have been updated.");
        }
        catch (Exception)
        {
            return _viewFactory.CreateNotification(NotificationType.Error, "Error: Something went wrong while updating the server settings.");
        }
    }
}
