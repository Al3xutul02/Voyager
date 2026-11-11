using BusinessLogic.Dtos.User;
using BusinessLogic.Json.Models;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;
using Voyager.API.Enums.Types;
using Voyager.API.Views.Components.Buttons;
using Voyager.API.Views.Components.Embeds;
using Voyager.API.Views.Components.Selects;

namespace Voyager.API.Views;

/// <summary>
/// Assembles complete <see cref="DiscordMessageBuilder"/> responses out of
/// the reusable building blocks in <c>Views.Components</c>. Slash commands
/// and interaction handlers depend on this rather than constructing
/// embeds/buttons/selects inline.
/// </summary>
/// <param name="isEphemeral">
/// Whether the message is sent ephemerally (in a server). When <c>false</c>
/// (e.g. in DMs), a manual "Clear" button is appended so the user can dismiss
/// the bot's message.
/// </param>
/// <param name="enumService">Used by every view that needs color or interaction-id conversion.</param>
/// <param name="settings">Server-wide UI defaults (colors for notifications, etc.).</param>
public class ViewFactory(
    bool isEphemeral,
    IEnumService enumService,
    ServerSettings settings)
{
    private readonly bool _isEphemeral = isEphemeral;
    private readonly ServerSettings _settings = settings;
    private readonly IEnumService _enumService = enumService;

    /// <summary>
    /// Builds a notification message: a colored embed plus a "Clear"
    /// dismissal button when non-ephemeral.
    /// </summary>
    /// <param name="notificationType">Picks which color from <see cref="ServerSettings"/> is used.</param>
    /// <param name="message">Text rendered as the embed title.</param>
    public DiscordMessageBuilder CreateNotification(NotificationType notificationType, string message)
    {
        var messageBuilder = new DiscordMessageBuilder()
            .AddEmbed(Embeds.Notification(_enumService, _settings, notificationType, message));

        if (!_isEphemeral)
            messageBuilder.AddComponents(Buttons.ClearMessage(_enumService, "Clear this notification"));

        return messageBuilder;
    }

    /// <summary>
    /// Builds the user-profile message used by <c>/profile view</c>:
    /// the profile embed, a page-switcher dropdown if the caller is viewing
    /// their own profile, and a "Clear" dismissal button when non-ephemeral.
    /// </summary>
    /// <param name="userDto">The user whose profile is being rendered.</param>
    /// <param name="callerId">The Discord user ID of the person who invoked the command.</param>
    public DiscordMessageBuilder CreateUserProfile(UserReadDto userDto, ulong callerId)
    {
        var messageBuilder = new DiscordMessageBuilder()
            .AddEmbed(Embeds.UserProfile(_enumService, userDto, callerId));

        if (callerId == userDto.Id)
            messageBuilder.AddComponents(Selects.SwitchProfilePage(_enumService, ProfilePage.Profile));

        if (!_isEphemeral)
            messageBuilder.AddComponents(Buttons.ClearMessage(_enumService, "Clear"));

        return messageBuilder;
    }

    /// <summary>
    /// Builds the user-settings message shown when a user switches their
    /// profile card to the Settings page: the settings embed, the page-switcher
    /// dropdown pre-selected to <see cref="ProfilePage.Settings"/>, and a
    /// "Clear" dismissal button when non-ephemeral.
    /// </summary>
    /// <param name="userDto">The user whose settings are being rendered.</param>
    public DiscordMessageBuilder CreateUserSettings(UserReadDto userDto)
    {
        var messageBuilder = new DiscordMessageBuilder()
            .AddEmbed(Embeds.UserSettings(_enumService, userDto))
            .AddComponents(Selects.SwitchProfilePage(_enumService, ProfilePage.Settings));

        if (!_isEphemeral)
            messageBuilder.AddComponents(Buttons.ClearMessage(_enumService, "Clear"));

        return messageBuilder;
    }
}
