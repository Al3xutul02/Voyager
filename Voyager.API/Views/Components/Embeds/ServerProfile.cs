using BusinessLogic.Dtos.Server;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;

namespace Voyager.API.Views.Components.Embeds;

public static partial class Embeds
{
    /// <summary>
    /// Builds the server profile embed shown by <c>/server view</c> and sent
    /// to the admin channel upon <c>/server setup</c>: displays the server's
    /// name, description (or a placeholder when none is set), general color,
    /// and operating mode in the footer.
    /// </summary>
    /// <param name="enumService">Used to translate the server's stored general palette color to a <see cref="DiscordColor"/>.</param>
    /// <param name="server">The server DTO whose profile is being rendered.</param>
    public static DiscordEmbedBuilder ServerProfile(IEnumService enumService, ServerReadDto server)
    {
        return new DiscordEmbedBuilder
        {
            Title = $"{server.Name}",
            Color = enumService.ConvertColor(server.Settings.GeneralColor),
            Description = server.Description ?? "_No description provided._",
        }
        .WithFooter($"Server Mode: {server.Settings.Mode}");
    }
}
