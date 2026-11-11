using BusinessLogic.Enums.Types;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;
using Voyager.API.Enums.Types;

namespace Voyager.API.Views.Components.Selects;

/// <summary>
/// Static factories for the bot's reusable select-menu components. Partial so
/// each select menu can live in its own file under <c>Views/Components/Selects</c>.
/// </summary>
public static partial class Selects
{
    /// <summary>
    /// Builds a dropdown select menu whose <c>custom_id</c> matches
    /// <see cref="InteractionIdType.SwitchProfilePage"/>, allowing a user
    /// viewing their own profile to switch between the Profile and Settings pages.
    /// </summary>
    /// <param name="enumService">Used to render the interaction id as its custom_id string.</param>
    /// <param name="currentPage">Which page is currently displayed (marked as default in the dropdown).</param>
    public static DiscordSelectComponent SwitchProfilePage(IEnumService enumService, ProfilePage currentPage)
    {
        var options = new DiscordSelectComponentOption[]
        {
            new(
                label: "Profile",
                value: nameof(ProfilePage.Profile),
                description: "View your user profile",
                isDefault: currentPage == ProfilePage.Profile),
            new(
                label: "Settings",
                value: nameof(ProfilePage.Settings),
                description: "View your user settings",
                isDefault: currentPage == ProfilePage.Settings)
        };

        return new DiscordSelectComponent(
            customId: enumService.ConvertInteraction(InteractionIdType.SwitchProfilePage),
            placeholder: "Select a page...",
            options: options);
    }
}
