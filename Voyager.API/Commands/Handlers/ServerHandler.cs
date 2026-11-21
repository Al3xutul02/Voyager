using BusinessLogic.Dtos.Server;
using BusinessLogic.Enums.Types;
using BusinessLogic.Json.Models;
using BusinessLogic.Services.Abstractions;
using DSharpPlus;
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
    /// Comma-separated list of all 42 valid palette colors from <see cref="Color"/>.
    /// Used for modal reference fields and error notification bodies.
    /// </summary>
    public static readonly string AllAvailableColors = string.Join(", ", Enum.GetNames<Color>());

    /// <summary>
    /// Parses a comma-separated string containing up to four colors (General, Error, Success, Warning).
    /// </summary>
    /// <param name="combinedColors">Comma-separated color names (e.g. "Teal, DarkRed, DarkGreen, Orange").</param>
    /// <returns>A tuple of (General, Error, Success, Warning) color strings.</returns>
    public static (string? General, string? Error, string? Success, string? Warning) ParseCombinedColors(string? combinedColors)
    {
        if (string.IsNullOrWhiteSpace(combinedColors))
        {
            return (null, null, null, null);
        }

        var parts = combinedColors
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        string? general = parts.Length > 0 ? parts[0] : null;
        string? error = parts.Length > 1 ? parts[1] : null;
        string? success = parts.Length > 2 ? parts[2] : null;
        string? warning = parts.Length > 3 ? parts[3] : null;

        return (general, error, success, warning);
    }

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

            _viewFactory.Settings = serverReadDto!.Settings;
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

        _viewFactory.Settings = server.Settings;
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
    /// <summary>
    /// Builds the Discord Modal response builder for changing server settings,
    /// pre-populated with the server's current settings, a combined single text
    /// input for all 4 colors, an in-modal reference list of all available colors,
    /// and optional pending GM role / admin channel IDs encoded into the custom_id.
    /// </summary>
    /// <param name="server">The server's current configuration.</param>
    /// <param name="gmRoleId">Optional pending new GM role ID.</param>
    /// <param name="adminChannelId">Optional pending new admin channel ID.</param>
    public DiscordInteractionResponseBuilder CreateChangeModal(
        ServerReadDto server,
        ulong? gmRoleId = null,
        ulong? adminChannelId = null)
    {
        var customId = $"{_enumService.ConvertInteraction(InteractionIdType.ServerChangeModal)}:{gmRoleId?.ToString() ?? "0"}:{adminChannelId?.ToString() ?? "0"}";

        return new DiscordInteractionResponseBuilder()
            .WithTitle("Change Server Settings")
            .WithCustomId(customId)
            .AddComponents(new TextInputComponent(
                label: "Description",
                customId: "server_description",
                placeholder: "Server description for profile card",
                value: server.Description,
                required: false,
                style: TextInputStyle.Paragraph,
                max_length: 500))
            .AddComponents(new TextInputComponent(
                label: "Colors (General, Error, Success, Warning)",
                customId: "server_colors",
                placeholder: "e.g. Teal, DarkRed, DarkGreen, Orange",
                value: $"{server.Settings.GeneralColor}, {server.Settings.ErrorColor}, {server.Settings.SuccessColor}, {server.Settings.WarningColor}",
                required: false,
                style: TextInputStyle.Short,
                max_length: 200))
            .AddComponents(new TextInputComponent(
                label: "Available Colors (Reference)",
                customId: "available_colors_reference",
                value: AllAvailableColors,
                required: false,
                style: TextInputStyle.Paragraph,
                max_length: 1000));
    }

    /// <summary>
    /// Builds the response for <c>/server change</c> when invoked with a single
    /// combined comma-separated colors string.
    /// </summary>
    public Task<DiscordMessageBuilder> ChangeAsync(
        ulong guildId,
        ulong? gmRoleId,
        ulong? adminChannelId,
        string? description,
        string? combinedColors)
    {
        var (general, error, success, warning) = ParseCombinedColors(combinedColors);
        return ChangeAsync(guildId, gmRoleId, adminChannelId, description, general, error, success, warning);
    }

    /// <summary>
    /// Builds the response for <c>/server change</c> when invoked with <see cref="DiscordRole"/>
    /// and <see cref="DiscordChannel"/> entities.
    /// </summary>
    public Task<DiscordMessageBuilder> ChangeAsync(
        ulong guildId,
        DiscordRole? gmRole,
        DiscordChannel? adminChannel,
        string? description,
        string? generalColor,
        string? errorColor,
        string? successColor,
        string? warningColor)
    {
        return ChangeAsync(
            guildId,
            gmRole?.Id,
            adminChannel?.Id,
            description,
            generalColor,
            errorColor,
            successColor,
            warningColor);
    }

    /// <summary>
    /// Builds the response for <c>/server change</c>: updates the server's
    /// GM role, admin channel, description, and UI palette colors.
    /// Returns a warning if no settings were provided, or an error if the server is not set up.
    /// </summary>
    /// <param name="guildId">The Discord snowflake ID of the guild.</param>
    /// <param name="gmRoleId">Optional new Game Master role snowflake ID.</param>
    /// <param name="adminChannelId">Optional new Voyager admin channel snowflake ID.</param>
    /// <param name="description">Optional new description for the server profile card.</param>
    /// <param name="generalColor">Optional new general palette color name.</param>
    /// <param name="errorColor">Optional new error notification palette color name.</param>
    /// <param name="successColor">Optional new success notification palette color name.</param>
    /// <param name="warningColor">Optional new warning notification palette color name.</param>
    public async Task<DiscordMessageBuilder> ChangeAsync(
        ulong guildId,
        ulong? gmRoleId,
        ulong? adminChannelId,
        string? description,
        string? generalColor,
        string? errorColor,
        string? successColor,
        string? warningColor)
    {
        if (string.IsNullOrEmpty(description) &&
            string.IsNullOrEmpty(generalColor) &&
            string.IsNullOrEmpty(errorColor) &&
            string.IsNullOrEmpty(successColor) &&
            string.IsNullOrEmpty(warningColor) &&
            gmRoleId is null &&
            adminChannelId is null)
        {
            return _viewFactory.CreateNotification(NotificationType.Warning, "Warning: No changes were provided.");
        }

        var server = await _serverService.GetByIdAsync(guildId);
        if (server is null)
        {
            return _viewFactory.CreateNotification(NotificationType.Error, "Error: This server is not set up yet.");
        }

        _viewFactory.Settings = server.Settings;

        if (!string.IsNullOrWhiteSpace(generalColor) && !Enum.TryParse<Color>(generalColor.Trim(), ignoreCase: true, out _))
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                $"Error: '{generalColor}' is not a valid color name.",
                $"**Available Colors:**\n{AllAvailableColors}");
        }
        if (!string.IsNullOrWhiteSpace(errorColor) && !Enum.TryParse<Color>(errorColor.Trim(), ignoreCase: true, out _))
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                $"Error: '{errorColor}' is not a valid color name.",
                $"**Available Colors:**\n{AllAvailableColors}");
        }
        if (!string.IsNullOrWhiteSpace(successColor) && !Enum.TryParse<Color>(successColor.Trim(), ignoreCase: true, out _))
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                $"Error: '{successColor}' is not a valid color name.",
                $"**Available Colors:**\n{AllAvailableColors}");
        }
        if (!string.IsNullOrWhiteSpace(warningColor) && !Enum.TryParse<Color>(warningColor.Trim(), ignoreCase: true, out _))
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                $"Error: '{warningColor}' is not a valid color name.",
                $"**Available Colors:**\n{AllAvailableColors}");
        }

        try
        {
            var updateDto = new ServerUpdateDto(
                Id: guildId,
                Name: server.Name,
                Description: description != null ? (string.IsNullOrWhiteSpace(description) ? null : description) : server.Description,
                GMRoleId: gmRoleId ?? server.GMRoleId,
                AdminChannelId: adminChannelId ?? server.AdminChannelId,
                Settings: new ServerSettings(
                    Mode: server.Settings.Mode,
                    GeneralColor: !string.IsNullOrWhiteSpace(generalColor) ? _enumService.ConvertColor(generalColor) : server.Settings.GeneralColor,
                    ErrorColor: !string.IsNullOrWhiteSpace(errorColor) ? _enumService.ConvertColor(errorColor) : server.Settings.ErrorColor,
                    SuccessColor: !string.IsNullOrWhiteSpace(successColor) ? _enumService.ConvertColor(successColor) : server.Settings.SuccessColor,
                    WarningColor: !string.IsNullOrWhiteSpace(warningColor) ? _enumService.ConvertColor(warningColor) : server.Settings.WarningColor
                )
            );

            await _serverService.UpdateAsync(updateDto);
            _viewFactory.Settings = updateDto.Settings;

            return _viewFactory.CreateNotification(NotificationType.Success, "Success: Server settings have been updated.");
        }
        catch (Exception)
        {
            return _viewFactory.CreateNotification(NotificationType.Error, "Error: Something went wrong while updating the server settings.");
        }
    }

    /// <summary>
    /// Builds the response for <c>/server colors</c>: returns an ephemeral card
    /// listing all available palette colors organized by category.
    /// </summary>
    /// <param name="guildId">The Discord snowflake ID of the guild.</param>
    public async Task<DiscordMessageBuilder> ColorsAsync(ulong guildId)
    {
        var server = await _serverService.GetByIdAsync(guildId);
        if (server is not null)
        {
            _viewFactory.Settings = server.Settings;
        }

        return _viewFactory.CreateServerColors();
    }
}
