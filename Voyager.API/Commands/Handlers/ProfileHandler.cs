using BusinessLogic.Dtos.User;
using BusinessLogic.Enums.Types;
using BusinessLogic.Json.Models;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;
using Voyager.API.Enums.Types;
using Voyager.API.Views;

namespace Voyager.API.Commands.Handlers;

/// <summary>
/// Testable core of the <c>/profile</c> command group. Takes primitive
/// inputs (user IDs, strings, enums) and produces a <see cref="DiscordMessageBuilder"/>,
/// keeping all branching logic out of the command methods — which can't be
/// unit-tested because DSharpPlus's <c>InteractionContext</c> and <c>DiscordUser</c>
/// aren't mockable.
/// </summary>
/// <param name="userService">Source of user data.</param>
/// <param name="enumService">Converts color names and interaction IDs.</param>
/// <param name="viewFactory">Builds the Discord message for the response.</param>
public class ProfileHandler(IUserService userService, IEnumService enumService, ViewFactory viewFactory)
{
    private readonly IUserService _userService = userService;
    private readonly IEnumService _enumService = enumService;
    private readonly ViewFactory _viewFactory = viewFactory;

    /// <summary>
    /// Builds the response for <c>/profile view</c>: the user's profile card
    /// if they exist (with the page-switcher dropdown when <paramref name="callerId"/>
    /// matches <paramref name="userId"/>), otherwise an error notification.
    /// </summary>
    /// <param name="userId">The Discord snowflake ID of the user whose profile to show.</param>
    /// <param name="callerId">The Discord snowflake ID of the user who invoked the command.</param>
    public async Task<DiscordMessageBuilder> ViewAsync(ulong userId, ulong callerId)
    {
        var result = await _userService.GetByIdAsync(userId);

        return result is null
            ? _viewFactory.CreateNotification(NotificationType.Error, "Error: User not found.")
            : _viewFactory.CreateUserProfile(result, callerId);
    }

    /// <summary>
    /// Builds the response for <c>/profile create</c>: creates a new profile
    /// with optional initial <paramref name="color"/> if the user does not
    /// already have one, or returns a warning notification if they do.
    /// </summary>
    /// <param name="userId">The Discord snowflake ID of the user creating their profile.</param>
    /// <param name="username">The Discord username to store on the profile.</param>
    /// <param name="color">Optional palette color name from autocomplete.</param>
    public async Task<DiscordMessageBuilder> CreateAsync(ulong userId, string username, string? color = null)
    {
        var result = await _userService.GetByIdAsync(userId);

        if (result is not null)
        {
            return _viewFactory.CreateNotification(NotificationType.Warning, "Warning: You already have a profile.");
        }

        try
        {
            var newSettings = !string.IsNullOrWhiteSpace(color)
                ? new UserSettings { Color = _enumService.ConvertColor(color) }
                : new UserSettings();

            await _userService.CreateAsync(new UserCreateDto(userId, username, newSettings));
            return _viewFactory.CreateNotification(NotificationType.Success, "Profile created successfully.");
        }
        catch (Exception)
        {
            return _viewFactory.CreateNotification(NotificationType.Error, "An error occurred while creating the profile.");
        }
    }

    /// <summary>
    /// Builds the response for <c>/profile change</c>: updates the caller's
    /// <see cref="UserSettings"/> with any non-null parameters provided.
    /// Returns a warning notification without hitting the database when all
    /// optional setting parameters are omitted.
    /// </summary>
    /// <param name="userId">The Discord snowflake ID of the user updating their settings.</param>
    /// <param name="username">The user's current Discord username.</param>
    /// <param name="color">Optional new palette color name from autocomplete.</param>
    /// <param name="gamePrivacy">Optional new privacy setting for games.</param>
    /// <param name="characterPrivacy">Optional new privacy setting for characters.</param>
    public async Task<DiscordMessageBuilder> ChangeAsync(
        ulong userId,
        string username,
        string? color = null,
        PrivacyOption? gamePrivacy = null,
        PrivacyOption? characterPrivacy = null)
    {
        if (string.IsNullOrWhiteSpace(color) && gamePrivacy is null && characterPrivacy is null)
        {
            return _viewFactory.CreateNotification(NotificationType.Warning, "Warning: No settings were provided to change.");
        }

        var result = await _userService.GetByIdAsync(userId);
        if (result is null)
        {
            return _viewFactory.CreateNotification(NotificationType.Error, "Error: User not found.");
        }

        try
        {
            var updatedSettings = result.Settings with
            {
                Color = !string.IsNullOrWhiteSpace(color) ? _enumService.ConvertColor(color) : result.Settings.Color,
                GamePrivacy = gamePrivacy ?? result.Settings.GamePrivacy,
                CharacterPrivacy = characterPrivacy ?? result.Settings.CharacterPrivacy
            };

            await _userService.UpdateAsync(new UserUpdateDto(userId, username, updatedSettings));
            return _viewFactory.CreateNotification(NotificationType.Success, "Settings updated successfully.");
        }
        catch (Exception)
        {
            return _viewFactory.CreateNotification(NotificationType.Error, "An error occurred while updating the settings.");
        }
    }

    /// <summary>
    /// Builds the user settings card for <paramref name="userId"/> (used when
    /// switching to the Settings page via the profile dropdown).
    /// </summary>
    /// <param name="userId">The Discord snowflake ID of the user whose settings to show.</param>
    public async Task<DiscordMessageBuilder> SettingsAsync(ulong userId)
    {
        var result = await _userService.GetByIdAsync(userId);
        return result is null
            ? _viewFactory.CreateNotification(NotificationType.Error, "Error: User not found.")
            : _viewFactory.CreateUserSettings(result);
    }
}
