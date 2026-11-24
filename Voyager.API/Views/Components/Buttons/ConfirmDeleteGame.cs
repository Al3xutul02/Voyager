using BusinessLogic.Enums.Types;
using BusinessLogic.Services.Abstractions;
using DSharpPlus;
using DSharpPlus.Entities;

namespace Voyager.API.Views.Components.Buttons;

public static partial class Buttons
{
    /// <summary>
    /// Builds a Danger button used to confirm permanent game deletion and channel archiving.
    /// Encodes both the game ID and the authorized caller user ID into the custom ID.
    /// </summary>
    /// <param name="enumService">Used to render the interaction id string.</param>
    /// <param name="gameId">The database ID of the game to delete.</param>
    /// <param name="authorizedUserId">The Discord user snowflake of the user who initiated deletion.</param>
    /// <param name="label">Label on the button face.</param>
    public static DiscordButtonComponent ConfirmDeleteGame(
        IEnumService enumService,
        ulong gameId,
        ulong authorizedUserId,
        string label = "Confirm Delete")
    {
        var baseId = enumService.ConvertInteraction(InteractionIdType.ConfirmDeleteGame);
        var customId = $"{baseId}:{gameId}:{authorizedUserId}";
        return new DiscordButtonComponent(
            ButtonStyle.Danger,
            customId,
            label,
            false,
            TrashEmoji);
    }
}
