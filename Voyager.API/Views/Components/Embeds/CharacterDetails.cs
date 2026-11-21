using System.Globalization;
using BusinessLogic.Dtos.Character;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;

namespace Voyager.API.Views.Components.Embeds;

public static partial class Embeds
{
    /// <summary>
    /// Builds the character details embed card matching the Pathfinder 2e sheet presentation:
    /// Ancestry/Class/Level author line, character name, description, token thumbnail,
    /// current game, age/gender/gold stats, full actor portrait, and owner footer.
    /// </summary>
    /// <param name="enumService">Used to convert the character's palette color.</param>
    /// <param name="character">The character DTO to render.</param>
    /// <param name="playerName">The Discord username of the player who owns the character.</param>
    /// <param name="hasTokenAttachment">Whether a token image file is attached with the name 'token.png'.</param>
    /// <param name="hasActorAttachment">Whether an actor image file is attached with the name 'actor.png'.</param>
    public static DiscordEmbedBuilder CharacterDetails(
        IEnumService enumService,
        CharacterReadDto character,
        string playerName,
        bool hasTokenAttachment = true,
        bool hasActorAttachment = true)
    {
        var ancestry = string.IsNullOrWhiteSpace(character.Details.Ancestry) ? "Unknown Ancestry" : character.Details.Ancestry;
        var characterClass = string.IsNullOrWhiteSpace(character.Details.Class) ? "Unknown Class" : character.Details.Class;
        var level = string.IsNullOrWhiteSpace(character.Details.Level) ? "1" : character.Details.Level;
        var header = $"{ancestry} {characterClass}, Level {level}";

        var embed = new DiscordEmbedBuilder
        {
            Author = new DiscordEmbedBuilder.EmbedAuthor { Name = header },
            Title = character.Name,
            Description = string.IsNullOrWhiteSpace(character.Description) ? null : character.Description,
            Color = enumService.ConvertColor(character.Color)
        };

        if (hasTokenAttachment)
        {
            embed.WithThumbnail("attachment://token.png");
        }

        embed.AddField("Current Game", character.CurrentGame ?? "None", false);
        embed.AddField("Age", string.IsNullOrWhiteSpace(character.Details.Age) ? "Unknown" : character.Details.Age, true);
        embed.AddField("Gender", string.IsNullOrWhiteSpace(character.Details.Gender) ? "Unknown" : character.Details.Gender, true);
        embed.AddField("Gold", character.Gold.ToString("0.##", CultureInfo.InvariantCulture), true);

        if (hasActorAttachment)
        {
            embed.WithImageUrl("attachment://actor.png");
        }

        embed.WithFooter($"Player: {playerName}");

        return embed;
    }
}
