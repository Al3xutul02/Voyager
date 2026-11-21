using BusinessLogic.Enums.Types;
using BusinessLogic.Json.Models;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;

namespace Voyager.API.Views.Components.Embeds;

public static partial class Embeds
{
    /// <summary>
    /// Builds the palette colors embed shown by <c>/server colors</c>.
    /// Groups all 42 named colors from <see cref="Color"/> into categorized
    /// fields for easy reference.
    /// </summary>
    /// <param name="enumService">Maps the general palette color to a <see cref="DiscordColor"/>.</param>
    /// <param name="settings">Server-wide color defaults.</param>
    public static DiscordEmbedBuilder ServerColors(IEnumService enumService, ServerSettings settings)
    {
        return new DiscordEmbedBuilder
        {
            Title = "🎨 Voyager Palette Colors",
            Description = "These are all the named colors supported by Voyager for server settings (`/server change`) and user profiles (`/profile change`).",
            Color = enumService.ConvertColor(settings.GeneralColor)
        }
        .AddField(
            "**Blues & Cyans**",
            "• Aquamarine\n• Azure\n• Blue\n• Blurple\n• CornflowerBlue\n• Cyan\n• DarkBlue\n• MidnightBlue\n• PhthaloBlue\n• Teal\n• Turquoise",
            true)
        .AddField(
            "**Greens**",
            "• Chartreuse\n• DarkGreen\n• Green\n• PhthaloGreen\n• SapGreen\n• SpringGreen",
            true)
        .AddField(
            "**Reds & Pinks**",
            "• DarkRed\n• HotPink\n• IndianRed\n• Magenta\n• Red\n• Rose",
            true)
        .AddField(
            "**Purples & Violets**",
            "• Lilac\n• Purple\n• Violet",
            true)
        .AddField(
            "**Warm & Earth Tones**",
            "• Brown\n• Gold\n• Goldenrod\n• Orange\n• Sienna\n• Wheat\n• Yellow",
            true)
        .AddField(
            "**Monochrome & Grayscale**",
            "• Black\n• DarkButNotBlack\n• DarkGray\n• Gray\n• Grayple\n• LightGray\n• NotQuiteBlack\n• VeryDarkGray\n• White",
            true)
        .WithFooter("Colors are case-insensitive. Use them in /server change or /profile change.");
    }
}
