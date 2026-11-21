using BusinessLogic.Enums.Types;
using BusinessLogic.Services.Abstractions;
using DSharpPlus;
using DSharpPlus.Entities;

namespace Voyager.API.Views.Components.Buttons;

public static partial class Buttons
{
    /// <summary>
    /// The floppy-disk emoji shown on the save roll button. Built from the raw
    /// unicode code point so no <see cref="DiscordClient"/> is required.
    /// </summary>
    private static readonly DiscordComponentEmoji SaveEmoji = new("💾");

    /// <summary>
    /// Builds a primary "Save" button whose <c>custom_id</c> matches
    /// <see cref="InteractionIdType.SaveRoll"/>. Clicking it opens a modal
    /// allowing any user to save the dice roll configuration to their profile.
    /// </summary>
    /// <param name="enumService">Used to render the interaction id as its custom_id string.</param>
    /// <param name="label">Label rendered on the button face.</param>
    public static DiscordButtonComponent SaveRoll(IEnumService enumService, string label = "Save")
    {
        return new DiscordButtonComponent(
            ButtonStyle.Primary,
            enumService.ConvertInteraction(InteractionIdType.SaveRoll),
            label,
            false,
            SaveEmoji);
    }
}
