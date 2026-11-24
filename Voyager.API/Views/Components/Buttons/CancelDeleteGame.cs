using BusinessLogic.Enums.Types;
using BusinessLogic.Services.Abstractions;
using DSharpPlus;
using DSharpPlus.Entities;

namespace Voyager.API.Views.Components.Buttons;

public static partial class Buttons
{
    /// <summary>
    /// Builds a Secondary button used to cancel game deletion.
    /// Encodes both the game ID and the authorized caller user ID into the custom ID.
    /// </summary>
    /// <param name="enumService">Used to render the interaction id string.</param>
    /// <param name="gameId">The database ID of the game.</param>
    /// <param name="authorizedUserId">The Discord user snowflake of the user who initiated deletion.</param>
    /// <param name="label">Label on the button face.</param>
    public static DiscordButtonComponent CancelDeleteGame(
        IEnumService enumService,
        ulong gameId,
        ulong authorizedUserId,
        string label = "Cancel")
    {
        var baseId = enumService.ConvertInteraction(InteractionIdType.CancelDeleteGame);
        var customId = $"{baseId}:{gameId}:{authorizedUserId}";
        return new DiscordButtonComponent(
            ButtonStyle.Secondary,
            customId,
            label,
            false,
            ClearEmoji);
    }
}
