using BusinessLogic.Enums.Types;
using BusinessLogic.Services.Abstractions;
using DSharpPlus;
using DSharpPlus.Entities;

namespace Voyager.API.Views.Components.Buttons;

/// <summary>
/// Static factories for the bot's reusable button components. Partial so
/// each button can live in its own file under <c>Views/Components/Buttons</c>.
/// </summary>
public static partial class Buttons
{
    /// <summary>
    /// The cross-mark emoji shown on the clear button. Built from the raw
    /// unicode code point so no <see cref="DiscordClient"/> is required —
    /// this keeps the button (and the views that use it) constructible in
    /// unit tests.
    /// </summary>
    private static readonly DiscordComponentEmoji ClearEmoji = new("❌"); // ❌

    /// <summary>
    /// Builds a secondary "clear" button whose <c>custom_id</c> matches
    /// <see cref="InteractionIdType.ClearAlert"/>. Clicking it triggers the
    /// <c>ClearAlert</c> handler in <c>EventHandler</c>, which deletes the
    /// message the button was attached to.
    /// </summary>
    /// <param name="enumService">Used to render the interaction id as its custom_id string.</param>
    /// <param name="label">Label rendered on the button face.</param>
    public static DiscordButtonComponent ClearMessage(IEnumService enumService, string label)
    {
        return new DiscordButtonComponent(
            ButtonStyle.Secondary,
            enumService.ConvertInteraction(InteractionIdType.ClearAlert),
            label,
            false,
            ClearEmoji);
    }
}
