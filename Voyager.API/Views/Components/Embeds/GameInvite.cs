using BusinessLogic.Dtos.Game;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;

namespace Voyager.API.Views.Components.Embeds;

public static partial class Embeds
{
    /// <summary>
    /// Builds the game invitation embed sent to an invited player's private DM when
    /// a Game Master runs <c>/game invite</c>.
    /// </summary>
    /// <param name="enumService">Used to convert the game's palette color.</param>
    /// <param name="game">The game DTO the player is invited to.</param>
    /// <param name="serverName">The name of the Discord server hosting the game.</param>
    /// <param name="hasThumbnailAttachment">Whether a thumbnail image file is attached with the name 'thumbnail.png'.</param>
    public static DiscordEmbedBuilder GameInvite(
        IEnumService enumService,
        GameReadDto game,
        string serverName,
        bool hasThumbnailAttachment = true)
    {
        var gmName = string.IsNullOrWhiteSpace(game.GameMasterName)
            ? $"<@{game.GameMasterId}>"
            : game.GameMasterName;

        var embed = new DiscordEmbedBuilder
        {
            Author = new DiscordEmbedBuilder.EmbedAuthor { Name = $"Game Invitation • {serverName}" },
            Title = game.Name,
            Description = string.IsNullOrWhiteSpace(game.Description) ? "_No description provided._" : game.Description,
            Color = enumService.ConvertColor(game.Color)
        };

        if (hasThumbnailAttachment)
        {
            embed.WithThumbnail("attachment://thumbnail.png");
        }

        embed.AddField("Game Master", gmName, true);
        embed.AddField("Server", serverName, true);
        embed.WithFooter("Use the buttons below to accept or decline this invitation.");

        return embed;
    }
}
