using BusinessLogic.Enums.Types;
using BusinessLogic.Services.Abstractions;
using DSharpPlus;
using DSharpPlus.Entities;

namespace Voyager.API.Views.Components.Buttons;

public static partial class Buttons
{
    private static readonly DiscordComponentEmoji CheckEmoji = new("✅");

    /// <summary>
    /// Builds a Success button used by an invited player in DMs to accept a game invitation.
    /// Encodes both the game ID and the invited user ID into the custom ID.
    /// </summary>
    /// <param name="enumService">Used to render the interaction id string.</param>
    /// <param name="gameId">The database ID of the game.</param>
    /// <param name="invitedUserId">The Discord user snowflake of the invited player.</param>
    /// <param name="label">Label on the button face.</param>
    public static DiscordButtonComponent AcceptGameInvite(
        IEnumService enumService,
        ulong gameId,
        ulong invitedUserId,
        string label = "Accept")
    {
        var baseId = enumService.ConvertInteraction(InteractionIdType.AcceptGameInvite);
        var customId = $"{baseId}:{gameId}:{invitedUserId}";
        return new DiscordButtonComponent(
            ButtonStyle.Success,
            customId,
            label,
            false,
            CheckEmoji);
    }
}
