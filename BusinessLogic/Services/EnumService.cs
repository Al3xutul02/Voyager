using BusinessLogic.Enums.Types;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;
using System.Collections.Frozen;

namespace BusinessLogic.Services;

/// <summary>
/// Implementation of <see cref="IEnumSerivce"/>. All lookups are backed by
/// <see cref="FrozenDictionary{TKey, TValue}"/> instances built at type-init,
/// so conversions are allocation-free and constant-time at call sites.
/// </summary>
public class EnumService : IEnumSerivce
{
    /// <summary>
    /// Forward palette: the only place colors are registered. To add a new
    /// color, add a value to the <see cref="Color"/> enum and a matching
    /// entry here. The reverse lookup updates automatically.
    /// </summary>
    private static readonly FrozenDictionary<Color, DiscordColor> Palette =
        new Dictionary<Color, DiscordColor>
        {
            [Color.Aquamarine]      = DiscordColor.Aquamarine,
            [Color.Azure]           = DiscordColor.Azure,
            [Color.Black]           = DiscordColor.Black,
            [Color.Blue]            = DiscordColor.Blue,
            [Color.Blurple]         = DiscordColor.Blurple,
            [Color.Brown]           = DiscordColor.Brown,
            [Color.Chartreuse]      = DiscordColor.Chartreuse,
            [Color.CornflowerBlue]  = DiscordColor.CornflowerBlue,
            [Color.Cyan]            = DiscordColor.Cyan,
            [Color.DarkBlue]        = DiscordColor.DarkBlue,
            [Color.DarkButNotBlack] = DiscordColor.DarkButNotBlack,
            [Color.DarkGray]        = DiscordColor.DarkGray,
            [Color.DarkGreen]       = DiscordColor.DarkGreen,
            [Color.DarkRed]         = DiscordColor.DarkRed,
            [Color.Gold]            = DiscordColor.Gold,
            [Color.Goldenrod]       = DiscordColor.Goldenrod,
            [Color.Gray]            = DiscordColor.Gray,
            [Color.Grayple]         = DiscordColor.Grayple,
            [Color.Green]           = DiscordColor.Green,
            [Color.HotPink]         = DiscordColor.HotPink,
            [Color.IndianRed]       = DiscordColor.IndianRed,
            [Color.LightGray]       = DiscordColor.LightGray,
            [Color.Lilac]           = DiscordColor.Lilac,
            [Color.Magenta]         = DiscordColor.Magenta,
            [Color.MidnightBlue]    = DiscordColor.MidnightBlue,
            [Color.NotQuiteBlack]   = DiscordColor.NotQuiteBlack,
            [Color.Orange]          = DiscordColor.Orange,
            [Color.PhthaloBlue]     = DiscordColor.PhthaloBlue,
            [Color.PhthaloGreen]    = DiscordColor.PhthaloGreen,
            [Color.Purple]          = DiscordColor.Purple,
            [Color.Red]             = DiscordColor.Red,
            [Color.Rose]            = DiscordColor.Rose,
            [Color.SapGreen]        = DiscordColor.SapGreen,
            [Color.Sienna]          = DiscordColor.Sienna,
            [Color.SpringGreen]     = DiscordColor.SpringGreen,
            [Color.Teal]            = DiscordColor.Teal,
            [Color.Turquoise]       = DiscordColor.Turquoise,
            [Color.VeryDarkGray]    = DiscordColor.VeryDarkGray,
            [Color.Violet]          = DiscordColor.Violet,
            [Color.Wheat]           = DiscordColor.Wheat,
            [Color.White]           = DiscordColor.White,
            [Color.Yellow]          = DiscordColor.Yellow,
        }.ToFrozenDictionary();

    /// <summary>
    /// Reverse lookup derived from <see cref="Palette"/>. Keyed on
    /// <see cref="DiscordColor.Value"/> (the underlying RGB int) so equality
    /// doesn't depend on DiscordColor's struct equality contract.
    /// </summary>
    private static readonly FrozenDictionary<int, Color> ReversePalette =
        Palette.ToFrozenDictionary(kv => kv.Value.Value, kv => kv.Key);

    private static readonly FrozenDictionary<InteractionIdType, string> InteractionMap =
        new Dictionary<InteractionIdType, string>
        {
            [InteractionIdType.None]       = "none", 
            [InteractionIdType.ClearAlert] = "clearAlert"
        }.ToFrozenDictionary();

    private static readonly FrozenDictionary<string, InteractionIdType> ReverseInteractionMap =
        InteractionMap.ToFrozenDictionary(kv => kv.Value, kv => kv.Key);

    /// <inheritdoc />
    public Color ConvertColor(DiscordColor color) =>
        ReversePalette.TryGetValue(color.Value, out var paletteColor) ? paletteColor : Color.Teal;

    /// <inheritdoc />
    public DiscordColor ConvertColor(Color color) =>
        Palette.TryGetValue(color, out var discordColor) ? discordColor : DiscordColor.Teal;

    /// <inheritdoc />
    public InteractionIdType ConvertInteraction(string interactionId) =>
        ReverseInteractionMap.TryGetValue(interactionId, out var interactionType) ? interactionType : InteractionIdType.None;

    /// <inheritdoc />
    public string ConvertInteraction(InteractionIdType interactionType) =>
        InteractionMap.TryGetValue(interactionType, out var interactionId) ? interactionId : "none";
}
