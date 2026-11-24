using BusinessLogic.Json.Models;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;

namespace Voyager.API.Views.Components.Embeds;

public static partial class Embeds
{
    /// <summary>
    /// Builds the warning confirmation embed shown when a user initiates <c>/game delete</c>.
    /// Explains that the action is irreversible, characters associated with the game will not be affected,
    /// and game channels will be archived.
    /// </summary>
    /// <param name="enumService">Used to convert the server warning color.</param>
    /// <param name="settings">Server UI settings containing the warning palette color.</param>
    /// <param name="gameName">The name of the game to be deleted.</param>
    public static DiscordEmbedBuilder GameDeleteConfirmation(
        IEnumService enumService,
        ServerSettings settings,
        string gameName)
    {
        return new DiscordEmbedBuilder
        {
            Title = $"Delete Game: {gameName}",
            Description = $"Are you sure you want to delete **{gameName}**?\n\n" +
                          "• This action is **irreversible**.\n" +
                          "• Characters associated with the game will **not** be affected.\n" +
                          "• Any channels associated with the game will be **archived**.",
            Color = enumService.ConvertColor(settings.WarningColor)
        };
    }
}
