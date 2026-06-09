using DSharpPlus;
using DSharpPlus.EventArgs;

namespace Voyager.API.Events;

public static class EventHandler
{
    public static async Task OnReady(DiscordClient sender, ReadyEventArgs args)
    {
        await Task.CompletedTask;
    }

    public static async Task GuildCreated(DiscordClient sender, GuildCreateEventArgs args)
    {
        await Task.CompletedTask;
    }

    public static async Task GuildDeleted(DiscordClient sender, GuildDeleteEventArgs args)
    {
        await Task.CompletedTask;
    }

    public static async Task GuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs args)
    {
        await Task.CompletedTask;
    }

    public static async Task GuildMemberRemoved(DiscordClient sender, GuildMemberRemoveEventArgs args)
    {
        await Task.CompletedTask;
    }

    public static async Task ComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs args)
    {
        await Task.CompletedTask;
    }
}
