using BusinessLogic.Enums.Types;
using BusinessLogic.Services.Abstractions;
using DSharpPlus;
using DSharpPlus.Entities;

namespace Voyager.API.Views.Components.Buttons;

public static partial class Buttons
{
    private static readonly DiscordComponentEmoji TrashEmoji = new("🗑️");

    /// <summary>
    /// Builds a Danger button used to confirm permanent character deletion.
    /// Encodes both the character ID and the authorized owner user ID into the custom ID.
    /// </summary>
    /// <param name="enumService">Used to render the interaction id string.</param>
    /// <param name="characterId">The database ID of the character to delete.</param>
    /// <param name="ownerUserId">The Discord user snowflake of the character owner.</param>
    /// <param name="label">Label on the button face.</param>
    public static DiscordButtonComponent ConfirmDeleteCharacter(
        IEnumService enumService,
        ulong characterId,
        ulong ownerUserId,
        string label = "Confirm Delete")
    {
        var baseId = enumService.ConvertInteraction(InteractionIdType.ConfirmDeleteCharacter);
        var customId = $"{baseId}:{characterId}:{ownerUserId}";
        return new DiscordButtonComponent(
            ButtonStyle.Danger,
            customId,
            label,
            false,
            TrashEmoji);
    }
}
