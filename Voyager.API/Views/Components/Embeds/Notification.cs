using BusinessLogic.Json.Models;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;
using Voyager.API.Enums.Types;

namespace Voyager.API.Views.Components.Embeds;

public static partial class Embeds
{
    /// <summary>
    /// Builds a single-line notification embed colored according to
    /// <paramref name="notificationType"/> using the corresponding color
    /// from <paramref name="settings"/>.
    /// </summary>
    /// <param name="enumService">Maps the chosen palette color to a <see cref="DiscordColor"/>.</param>
    /// <param name="settings">Server-wide color defaults.</param>
    /// <param name="notificationType">Determines which color from <paramref name="settings"/> is used.</param>
    /// <param name="message">The text rendered as the embed title.</param>
    /// <exception cref="ArgumentException">Thrown for any unrecognized <paramref name="notificationType"/>.</exception>
    public static DiscordEmbedBuilder Notification(
        IEnumService enumService, ServerSettings settings, NotificationType notificationType, string message)
    {
        return notificationType switch
        {
            NotificationType.General => new DiscordEmbedBuilder
            {
                Title = message,
                Color = enumService.ConvertColor(settings.GeneralColor)
            },
            NotificationType.Error => new DiscordEmbedBuilder
            {
                Title = message,
                Color = enumService.ConvertColor(settings.ErrorColor)
            },
            NotificationType.Success => new DiscordEmbedBuilder
            {
                Title = message,
                Color = enumService.ConvertColor(settings.SuccessColor)
            },
            _ => throw new ArgumentException("Invalid notification type, only General, Error and Success are supported"),
        };
    }
}
