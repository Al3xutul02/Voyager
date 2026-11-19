using BusinessLogic.Dtos.Server;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;

namespace Voyager.API.Views.Components.Embeds;

public static partial class Embeds
{
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
