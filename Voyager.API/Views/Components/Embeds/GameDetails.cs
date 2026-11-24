using BusinessLogic.Dtos.Game;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;

namespace Voyager.API.Views.Components.Embeds;

public static partial class Embeds
{
    /// <summary>
    /// Builds the game details embed card shown by <c>/game details</c>:
    /// game name, description, game master's name, list of players in the game,
    /// the game's palette color, and the game's thumbnail image.
    /// </summary>
    /// <param name="enumService">Used to convert the game's palette color.</param>
    /// <param name="game">The game DTO to render.</param>
    /// <param name="hasThumbnailAttachment">Whether a thumbnail image file is attached with the name 'thumbnail.png'.</param>
    public static DiscordEmbedBuilder GameDetails(
        IEnumService enumService,
        GameReadDto game,
        bool hasThumbnailAttachment = true)
    {
        var gmName = string.IsNullOrWhiteSpace(game.GameMasterName)
            ? $"<@{game.GameMasterId}>"
            : game.GameMasterName;

        var playersList = game.Players is not null && game.Players.Count > 0
            ? string.Join("\n", game.Players.Select(p => $"• {p.Name}"))
            : "_No players yet_";

        var embed = new DiscordEmbedBuilder
        {
            Title = game.Name,
            Description = string.IsNullOrWhiteSpace(game.Description) ? "_No description provided._" : game.Description,
            Color = enumService.ConvertColor(game.Color)
        };

        if (hasThumbnailAttachment)
        {
            embed.WithThumbnail("attachment://thumbnail.png");
        }

        embed.AddField("Game Master", gmName, false);
        embed.AddField("Players", playersList, false);

        return embed;
    }
}
