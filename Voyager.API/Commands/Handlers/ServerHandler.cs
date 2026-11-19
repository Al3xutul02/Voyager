using BusinessLogic.Dtos.Server;
using BusinessLogic.Enums.Types;
using BusinessLogic.Json.Models;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;
using Voyager.API.Enums.Types;
using Voyager.API.Views;

namespace Voyager.API.Commands.Handlers;

public class ServerHandler(IServerService serverService, IEnumService enumService, ViewFactory viewFactory)
{
    private readonly IServerService _serverService = serverService;
    private readonly IEnumService _enumService = enumService;
    private readonly ViewFactory _viewFactory = viewFactory;

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

        if (createGmRole && gmRole is null)
        {
            return _viewFactory.CreateNotification(NotificationType.Error, "Error: GM role creation requested but no role provided.");
        }

        if (createAdminChannel && adminChannel is null)
        {
            return _viewFactory.CreateNotification(NotificationType.Error, "Error: Admin channel creation requested but no channel provided.");
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
                adminChannel = await guild.CreateChannelAsync("voyager-admin", DSharpPlus.ChannelType.Private, reason: "Voyager admin channel creation");
            }
            
            await adminChannel!.AddOverwriteAsync(guild.Owner, allow: DSharpPlus.Permissions.All, deny: DSharpPlus.Permissions.None);

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
            return _viewFactory.CreateNotification(NotificationType.Error, $"Error: Something went wrong while setting up Voyager.");
        }
    }

    public async Task<DiscordMessageBuilder> ViewAsync(ulong guildId)
    {
        var server = await _serverService.GetByIdAsync(guildId);
        if (server is null)
        {
            return _viewFactory.CreateNotification(NotificationType.Error, "Error: This server is not set up yet.");
        }
        return _viewFactory.CreateServerProfile(server);
    }

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
