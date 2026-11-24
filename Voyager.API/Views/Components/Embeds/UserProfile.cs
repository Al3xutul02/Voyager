using BusinessLogic.Dtos.Character;
using BusinessLogic.Dtos.Game;
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
    /// and lists for games and characters (respecting privacy settings
    /// when viewed by someone other than the profile owner).
    /// </summary>
    /// <param name="enumService">Used to translate the stored palette color to a <see cref="DiscordColor"/>.</param>
    /// <param name="userDto">The user whose profile is being rendered.</param>
    /// <param name="callerId">The Discord user ID of the person viewing the profile.</param>
    /// <param name="characters">Optional collection of active characters owned by the user.</param>
    /// <param name="games">Optional collection of active games the user runs or plays in.</param>
    public static DiscordEmbedBuilder UserProfile(
        IEnumService enumService,
        UserReadDto userDto,
        ulong callerId,
        IEnumerable<CharacterReadDto>? characters = null,
        IEnumerable<GameReadDto>? games = null)
    {
        string gameList;
        if (callerId == userDto.Id || userDto.Settings.GamePrivacy == PrivacyOption.Public)
        {
            var gList = games?.ToList();
            if (gList is not null && gList.Count > 0)
            {
                gameList = string.Join("\n", gList.Select(g =>
                {
                    var role = g.GameMasterId == userDto.Id ? "GM" : "Player";
                    return $"• {g.Name} ({role})";
                }));
            }
            else
            {
                gameList = "_No Current Games_";
            }
        }
        else
        {
            gameList = "_Games hidden by user_";
        }

        string characterList;
        if (callerId == userDto.Id || userDto.Settings.CharacterPrivacy == PrivacyOption.Public)
        {
            var charList = characters?.ToList();
            if (charList is not null && charList.Count > 0)
            {
                characterList = string.Join("\n", charList.Select(c =>
                {
                    var ancestry = string.IsNullOrWhiteSpace(c.Details.Ancestry) ? "Unknown" : c.Details.Ancestry;
                    var cls = string.IsNullOrWhiteSpace(c.Details.Class) ? "Unknown" : c.Details.Class;
                    var lvl = string.IsNullOrWhiteSpace(c.Details.Level) ? "1" : c.Details.Level;
                    return $"• {c.Name} ({ancestry} {cls} {lvl})";
                }));
            }
            else
            {
                characterList = "_No Current Characters_";
            }
        }
        else
        {
            characterList = "_Characters hidden by user_";
        }

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
