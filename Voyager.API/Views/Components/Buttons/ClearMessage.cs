using DSharpPlus;
using DSharpPlus.Entities;

namespace Voyager.API.Views.Components.Buttons;

public static partial class Buttons
{
    public static DiscordButtonComponent ClearMessage(string label)
    {
        return new DiscordButtonComponent(
            ButtonStyle.Secondary,
            "clearAlert",
            label,
            false,
            new DiscordComponentEmoji(DiscordEmoji.FromName(Program.DiscordClient, ":x:")));
    }
}
