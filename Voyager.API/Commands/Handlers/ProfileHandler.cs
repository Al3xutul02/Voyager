using BusinessLogic.Dtos.User;
using BusinessLogic.Json.Models;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;
using Voyager.API.Enums.Types;
using Voyager.API.Views;

namespace Voyager.API.Commands.Handlers;

/// <summary>
/// Testable core of the <c>/profile</c> command group. Takes primitive
/// inputs (a user id) and produces a <see cref="DiscordMessageBuilder"/>,
/// keeping all branching logic out of the command method — which can't be
/// unit-tested because DSharpPlus's <c>InteractionContext</c> isn't mockable.
/// </summary>
/// <param name="userService">Source of user data.</param>
/// <param name="viewFactory">Builds the Discord message for the response.</param>
public class ProfileHandler(IUserService userService, IEnumService enumService, ViewFactory viewFactory)
{
    private readonly IUserService _userService = userService;
    private readonly IEnumService _enumService = enumService;
    private readonly ViewFactory _viewFactory = viewFactory;

    /// <summary>
    /// Builds the response for <c>/profile view</c>: the user's profile card
    /// if they exist, otherwise an error notification.
    /// </summary>
    /// <param name="user">The Discord user whose profile to show.</param>
    public async Task<DiscordMessageBuilder> ViewAsync(DiscordUser user)
    {
        var result = await _userService.GetByIdAsync(user.Id);

        return result is null
            ? _viewFactory.CreateNotification(NotificationType.Error, "Error: User not found.")
            : _viewFactory.CreateUserProfile(result);
    }

    public async Task<DiscordMessageBuilder> CreateAsync(DiscordUser user, DiscordColor? color)
    {
        var result = await _userService.GetByIdAsync(user.Id);

        if (result is not null)
        {
            return _viewFactory.CreateNotification(NotificationType.Warning, "Warning: You already have a profile.");
        }

        try
        {
            var newSettings = color is not null
                ? new UserSettings { Color = _enumService.ConvertColor(color.Value) }
                : new UserSettings();

            await _userService.CreateAsync(new UserCreateDto(user.Id, user.Username, newSettings));
            return _viewFactory.CreateNotification(NotificationType.Success, "Profile created successfully.");
        }
        catch (Exception)
        {
            return _viewFactory.CreateNotification(NotificationType.Error, $"An error occurred while creating the profile.");
        }
    }
}
