using BusinessLogic.Dtos.User;
using BusinessLogic.Enums.Types;
using BusinessLogic.Json.Models;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;
using Voyager.API.Enums.Types;

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
    /// and placeholder lists for games and characters until those features
    /// are implemented.
    /// </summary>
    /// <param name="enumService">Used to translate the stored palette color to a <see cref="DiscordColor"/>.</param>
    /// <param name="userDto">The user whose profile is being rendered.</param>
    public static DiscordEmbedBuilder UserProfile(
        IEnumService enumService, UserReadDto userDto, DiscordUser caller)
    {
        string gameList, characterList;

        // Construct game list
        if (caller.Id == userDto.Id || userDto.Settings.GamePrivacy == PrivacyOption.Public)
        {
            
            gameList = "_No Current Games_";
        }
        else
        {
            gameList = "_Games hidden by user_";
        }

        // Construct character list
        if (caller.Id == userDto.Id || userDto.Settings.CharacterPrivacy == PrivacyOption.Public)
        {
            characterList = "_No Current Characters_";
        }
        else
        {
            characterList = "_Characters hidden by user_";
        }

        // Construct player card
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
