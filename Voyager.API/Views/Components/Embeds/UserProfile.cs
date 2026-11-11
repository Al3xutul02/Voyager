using BusinessLogic.Dtos.User;
using BusinessLogic.Enums.Types;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;

namespace Voyager.API.Views.Components.Embeds;

/// <summary>
/// Static factories for the bot's reusable embed components. Partial so
/// each embed can live in its own file under <c>Views/Components/Embeds</c>.
/// </summary>
public static partial class Embeds
{
    /// <summary>
    /// Builds the user profile embed shown by <c>/profile view</c>:
    /// title from the user's name, color from their saved palette choice,
    /// and placeholder lists for games and characters (respecting privacy
    /// settings when viewed by someone other than the profile owner).
    /// </summary>
    /// <param name="enumService">Used to translate the stored palette color to a <see cref="DiscordColor"/>.</param>
    /// <param name="userDto">The user whose profile is being rendered.</param>
    /// <param name="callerId">The Discord user ID of the person viewing the profile.</param>
    public static DiscordEmbedBuilder UserProfile(
        IEnumService enumService, UserReadDto userDto, ulong callerId)
    {
        string gameList = callerId == userDto.Id || userDto.Settings.GamePrivacy == PrivacyOption.Public
            ? "_No Current Games_"
            : "_Games hidden by user_";

        string characterList = callerId == userDto.Id || userDto.Settings.CharacterPrivacy == PrivacyOption.Public
            ? "_No Current Characters_"
            : "_Characters hidden by user_";

        return new DiscordEmbedBuilder
        {
            Title = $"{userDto.Name}'s Profile",
            Color = enumService.ConvertColor(userDto.Settings.Color)
        }
        .WithFooter($"Profile Color: {userDto.Settings.Color}")
        .AddField("**Current Games**", gameList, true)
        .AddField("**Current Characters**", characterList, true);
    }
}
