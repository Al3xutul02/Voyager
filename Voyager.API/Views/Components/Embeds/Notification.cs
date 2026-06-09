using BusinessLogic.Json.Models;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;
using Voyager.API.Enums.Types;

namespace Voyager.API.Views.Components.Embeds;

public static partial class Embeds
{
    public static DiscordEmbedBuilder Notification(
        IMediaSerivce mediaService, ServerSettings settings, NotificationType notificationType, string message)
    {
        return notificationType switch
        {
            NotificationType.General => new DiscordEmbedBuilder
            {
                Title = message,
                Color = mediaService.ConvertColor(settings.GeneralColor)
            },
            NotificationType.Error => new DiscordEmbedBuilder
            {
                Title = message,
                Color = mediaService.ConvertColor(settings.ErrorColor)
            },
            NotificationType.Success => new DiscordEmbedBuilder
            {
                Title = message,
                Color = mediaService.ConvertColor(settings.SuccessColor)
            },
            _ => throw new ArgumentException("Invalid notification type, only General, Error and Success are supported"),
        };
    }
}
