using BusinessLogic.Enums.Types;
using DSharpPlus.Entities;

namespace BusinessLogic.Services.Abstractions;

/// <summary>
/// Provides bidirectional translation between the bot's domain enums
/// (<see cref="Color"/>, <see cref="InteractionIdType"/>) and their
/// external representations (DSharpPlus colors, component custom_id strings).
/// </summary>
public interface IEnumService
{
    /// <summary>
    /// Map a DSharpPlus <see cref="DiscordColor"/> to the matching palette entry.
    /// Falls back to a default color when the input isn't part of the palette.
    /// </summary>
    public Color ConvertColor(DiscordColor color);

    /// <summary>
    /// Map a palette <see cref="Color"/> to its DSharpPlus equivalent.
    /// Falls back to a default color when the input isn't registered.
    /// </summary>
    public DiscordColor ConvertColor(Color color);

    /// <summary>
    /// Parse a component's <c>custom_id</c> string into its
    /// <see cref="InteractionIdType"/>. Unknown ids resolve to
    /// <see cref="InteractionIdType.None"/>.
    /// </summary>
    public InteractionIdType ConvertInteraction(string interactionId);

    /// <summary>
    /// Render an <see cref="InteractionIdType"/> back to the string used
    /// in the component's <c>custom_id</c>.
    /// </summary>
    public string ConvertInteraction(InteractionIdType interactionType);
}
