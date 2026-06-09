using BusinessLogic.Dtos.User;
using BusinessLogic.Json.Models;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;
using Voyager.API.Enums.Types;
using Voyager.API.Views.Components.Buttons;
using Voyager.API.Views.Components.Embeds;

namespace Voyager.API.Views;

/// <summary>
/// Assembles complete <see cref="DiscordMessageBuilder"/> responses out of
/// the reusable building blocks in <c>Views.Components</c>. Slash commands
/// and interaction handlers depend on this rather than constructing
/// embeds/buttons inline.
/// </summary>
/// <param name="enumService">Used by every view that needs color or interaction-id conversion.</param>
/// <param name="settings">Server-wide UI defaults (colors for notifications, etc.).</param>
public class ViewFactory(
    IEnumSerivce enumService,
    ServerSettings settings)
{
    private readonly ServerSettings _settings = settings;
    private readonly IEnumSerivce _enumSerivce = enumService;

    /// <summary>
    /// Builds a notification message: a colored embed plus a "Clear"
    /// dismissal button.
    /// </summary>
    /// <param name="notificationType">Picks which color from <see cref="ServerSettings"/> is used.</param>
    /// <param name="message">Text rendered as the embed title.</param>
    public DiscordMessageBuilder CreateNotification(NotificationType notificationType, string message)
    {
        return new DiscordMessageBuilder()
            .AddEmbed(Embeds.Notification(_enumSerivce, _settings, notificationType, message))
            .AddComponents(Buttons.ClearMessage(_enumSerivce, "Clear this notification"));
    }

    /// <summary>
    /// Builds the user-profile message used by <c>/profile view</c>:
    /// the profile embed plus a "Clear" dismissal button.
    /// </summary>
    public DiscordMessageBuilder CreateUserProfile(UserReadDto userDto)
    {
        return new DiscordMessageBuilder()
            .AddEmbed(Embeds.UserProfile(_enumSerivce, userDto))
            .AddComponents(Buttons.ClearMessage(_enumSerivce, "Clear"));
    }
}
