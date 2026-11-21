using BusinessLogic.Enums.Types;
using BusinessLogic.Services.Abstractions;
using DSharpPlus;
using DSharpPlus.Entities;

namespace Voyager.API.Views.Components.Buttons;

public static partial class Buttons
{
    /// <summary>
    /// Builds a secondary "Clear" button for dice roll messages whose <c>custom_id</c>
    /// matches <see cref="InteractionIdType.ClearRoll"/>. Clicking it deletes the roll
    /// message only if the clicker is the user who rolled, a server GM, or a server Admin.
    /// </summary>
    /// <param name="enumService">Used to render the interaction id as its custom_id string.</param>
    /// <param name="label">Label rendered on the button face.</param>
    public static DiscordButtonComponent ClearRoll(IEnumService enumService, string label = "Clear")
    {
        return new DiscordButtonComponent(
            ButtonStyle.Secondary,
            enumService.ConvertInteraction(InteractionIdType.ClearRoll),
            label,
            false,
            ClearEmoji);
    }
}
