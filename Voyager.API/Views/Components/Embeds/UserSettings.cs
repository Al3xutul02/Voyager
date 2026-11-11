using BusinessLogic.Dtos.User;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;

namespace Voyager.API.Views.Components.Embeds;

public static partial class Embeds
{
    /// <summary>
    /// Builds the user settings embed showing the user's customization
    /// (profile color) and privacy settings (game and character visibility).
    /// </summary>
    /// <param name="enumService">Used to translate the stored palette color to a <see cref="DiscordColor"/>.</param>
    /// <param name="userDto">The user whose settings are being rendered.</param>
    public static DiscordEmbedBuilder UserSettings(IEnumService enumService, UserReadDto userDto)
    {
        string customizationList = $"**Profile Color:** {userDto.Settings.Color}";

        string privacyList = $"**Game Privacy:** {userDto.Settings.GamePrivacy}\n" +
                             $"**Character Privacy:** {userDto.Settings.CharacterPrivacy}\n";

        return new DiscordEmbedBuilder
        {
            Title = $"{userDto.Name}'s Settings",
            Color = enumService.ConvertColor(userDto.Settings.Color)
        }
        .WithFooter($"Profile Color: {userDto.Settings.Color}")
        .AddField("**Customization Settings**", customizationList, true)
        .AddField("**Privacy Settings**", privacyList, true);
    }
}
