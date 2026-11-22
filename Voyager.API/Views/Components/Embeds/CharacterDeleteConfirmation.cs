using BusinessLogic.Json.Models;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;

namespace Voyager.API.Views.Components.Embeds;

public static partial class Embeds
{
    /// <summary>
    /// Builds the warning confirmation embed shown when a user initiates <c>/character delete</c>.
    /// Warns the user that the action is permanent and irreversible.
    /// </summary>
    /// <param name="enumService">Used to convert the server warning color.</param>
    /// <param name="settings">Server UI settings containing the warning palette color.</param>
    /// <param name="characterName">The name of the character to be deleted.</param>
    public static DiscordEmbedBuilder CharacterDeleteConfirmation(
        IEnumService enumService,
        ServerSettings settings,
        string characterName)
    {
        return new DiscordEmbedBuilder
        {
            Title = $"Delete Character: {characterName}",
            Description = $"Are you sure you want to delete **{characterName}**? This action is permanent and irreversible.",
            Color = enumService.ConvertColor(settings.WarningColor)
        };
    }
}
