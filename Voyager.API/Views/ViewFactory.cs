using BusinessLogic.Json.Models;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;
using Voyager.API.Enums.Types;
using Voyager.API.Views.Components.Buttons;
using Voyager.API.Views.Components.Embeds;

namespace Voyager.API.Views;

public class ViewFactory(
    IMediaSerivce mediaSerivce,
    ServerSettings settings)
{
    private readonly ServerSettings _settings = settings;
    private readonly IMediaSerivce _mediaSerivce = mediaSerivce;
    public DiscordMessageBuilder CreateNotification(NotificationType notificationType, string message)
    {
        return new DiscordMessageBuilder()
            .AddEmbed(Embeds.Notification(_mediaSerivce, _settings, notificationType, message))
            .AddComponents(Buttons.ClearMessage("Clear this notification"));
    }
}
