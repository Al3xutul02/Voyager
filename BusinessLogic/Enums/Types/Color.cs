using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BusinessLogic.Enums.Types;

/// <summary>
/// Bot-wide named color palette. Each value corresponds 1:1 to a DSharpPlus
/// <see cref="DSharpPlus.Entities.DiscordColor"/> via <c>EnumService.Palette</c>.
/// Serialized as its string name (e.g. <c>"Gray"</c>) so JSON columns stay
/// human-readable.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum Color
{
    Aquamarine,
    Azure,
    Black,
    Blue,
    Blurple,
    Brown,
    Chartreuse,
    CornflowerBlue,
    Cyan,
    DarkBlue,
    DarkButNotBlack,
    DarkGray,
    DarkGreen,
    DarkRed,
    Gold,
    Goldenrod,
    Gray,
    Grayple,
    Green,
    HotPink,
    IndianRed,
    LightGray,
    Lilac,
    Magenta,
    MidnightBlue,
    NotQuiteBlack,
    Orange,
    PhthaloBlue,
    PhthaloGreen,
    Purple,
    Red,
    Rose,
    SapGreen,
    Sienna,
    SpringGreen,
    Teal,
    Turquoise,
    VeryDarkGray,
    Violet,
    Wheat,
    White,
    Yellow,
}
