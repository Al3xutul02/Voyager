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
public class ProfileHandler(IUserService userService, ViewFactory viewFactory)
{
    private readonly IUserService _userService = userService;
    private readonly ViewFactory _viewFactory = viewFactory;

    /// <summary>
    /// Builds the response for <c>/profile view</c>: the user's profile card
    /// if they exist, otherwise an error notification.
    /// </summary>
    /// <param name="userId">The Discord id of the user whose profile to show.</param>
    public async Task<DiscordMessageBuilder> BuildProfileViewAsync(ulong userId)
    {
        var user = await _userService.GetByIdAsync(userId);

        return user is null
            ? _viewFactory.CreateNotification(NotificationType.Error, "Error: User not found.")
            : _viewFactory.CreateUserProfile(user);
    }
}
