using BusinessLogic.Dtos.User;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;

namespace Voyager.API.Views.Components.Embeds;

public static partial class Embeds
{
    public static DiscordEmbedBuilder UserSettings(IEnumService enumService, UserReadDto userDto)
    {
        // Construct customization settings
        string customizationList = $"**Profile Color:** {userDto.Settings.Color}";

        // Construct privacy settings
        string privacyList = $"**Game Privacy:** {userDto.Settings.GamePrivacy}\n" +
                             $"**Character Privacy:** {userDto.Settings.CharacterPrivacy}\n";

        // Construct settings card
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