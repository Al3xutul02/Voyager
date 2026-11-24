using BusinessLogic.Enums.Types;
using BusinessLogic.Services.Abstractions;
using DSharpPlus;
using DSharpPlus.Entities;

namespace Voyager.API.Views.Components.Buttons;

public static partial class Buttons
{
    /// <summary>
    /// Builds a Danger button used by an invited player in DMs to decline a game invitation.
    /// Encodes both the game ID and the invited user ID into the custom ID.
    /// </summary>
    /// <param name="enumService">Used to render the interaction id string.</param>
    /// <param name="gameId">The database ID of the game.</param>
    /// <param name="invitedUserId">The Discord user snowflake of the invited player.</param>
    /// <param name="label">Label on the button face.</param>
    public static DiscordButtonComponent DeclineGameInvite(
        IEnumService enumService,
        ulong gameId,
        ulong invitedUserId,
        string label = "Deny")
    {
        var baseId = enumService.ConvertInteraction(InteractionIdType.DeclineGameInvite);
        var customId = $"{baseId}:{gameId}:{invitedUserId}";
        return new DiscordButtonComponent(
            ButtonStyle.Danger,
            customId,
            label,
            false,
            ClearEmoji);
    }
}
