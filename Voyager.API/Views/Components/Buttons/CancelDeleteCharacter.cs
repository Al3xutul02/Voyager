using BusinessLogic.Enums.Types;
using BusinessLogic.Services.Abstractions;
using DSharpPlus;
using DSharpPlus.Entities;

namespace Voyager.API.Views.Components.Buttons;

public static partial class Buttons
{
    /// <summary>
    /// Builds a Secondary button used to cancel character deletion.
    /// Encodes both the character ID and the authorized owner user ID into the custom ID.
    /// </summary>
    /// <param name="enumService">Used to render the interaction id string.</param>
    /// <param name="characterId">The database ID of the character.</param>
    /// <param name="ownerUserId">The Discord user snowflake of the character owner.</param>
    /// <param name="label">Label on the button face.</param>
    public static DiscordButtonComponent CancelDeleteCharacter(
        IEnumService enumService,
        ulong characterId,
        ulong ownerUserId,
        string label = "Cancel")
    {
        var baseId = enumService.ConvertInteraction(InteractionIdType.CancelDeleteCharacter);
        var customId = $"{baseId}:{characterId}:{ownerUserId}";
        return new DiscordButtonComponent(
            ButtonStyle.Secondary,
            customId,
            label,
            false,
            ClearEmoji);
    }
}
