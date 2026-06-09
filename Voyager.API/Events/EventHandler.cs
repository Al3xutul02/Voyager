using BusinessLogic.Enums.Types;
using BusinessLogic.Services.Abstractions;
using DSharpPlus;
using DSharpPlus.EventArgs;

namespace Voyager.API.Events;

/// <summary>
/// Central host for the bot's gateway event subscriptions. Wired up in
/// <c>Program.cs</c> on the singleton <see cref="DiscordClient"/>. Keeps the
/// per-event handler methods static so <c>DiscordClient.Event += ...</c>
/// reads cleanly at the call site; dependency resolution goes through the
/// <see cref="_scopeFactory"/> captured by <see cref="Initialize"/>.
/// </summary>
public static partial class EventHandler
{
    /// <summary>
    /// Captured once at startup so the static event handlers can open DI
    /// scopes to resolve scoped services (e.g. <see cref="IEnumService"/>).
    /// Call <see cref="Initialize"/> from Program.cs after the app is built.
    /// </summary>
    private static IServiceScopeFactory? _scopeFactory;

    /// <summary>
    /// Routing table from a parsed <see cref="InteractionIdType"/> to the
    /// async handler that should run for that component. The <c>None</c>
    /// entry throws on purpose so unrecognized custom_ids surface in the
    /// dispatcher's catch block as logged errors rather than silent no-ops.
    /// </summary>
    public static readonly Dictionary<InteractionIdType, Func<
        DiscordClient, ComponentInteractionCreateEventArgs, IServiceProvider, Task>>
        _interactionDispatchMap = new()
        {
            [InteractionIdType.None] = (sender, args, services) =>
                throw new ArgumentException("Interaction id does not exist"),
            [InteractionIdType.ClearAlert] = ClearAlert
        };

    /// <summary>
    /// Provides the static handlers with access to the DI container.
    /// Must be called exactly once at startup before any event fires.
    /// </summary>
    public static void Initialize(IServiceScopeFactory scopeFactory) =>
        _scopeFactory = scopeFactory;

    /// <summary>
    /// Fires once the gateway has finished its initial READY handshake.
    /// Placeholder — extend when startup-time work is needed.
    /// </summary>
    public static async Task OnReady(DiscordClient sender, ReadyEventArgs args)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Fires when the bot joins a new guild (or finishes resuming one).
    /// Placeholder — extend with onboarding/setup logic when needed.
    /// </summary>
    public static async Task GuildCreated(DiscordClient sender, GuildCreateEventArgs args)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Fires when the bot is removed from a guild or the guild becomes
    /// unavailable. Placeholder — extend with cleanup logic when needed.
    /// </summary>
    public static async Task GuildDeleted(DiscordClient sender, GuildDeleteEventArgs args)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Fires when a new member joins a guild the bot is in.
    /// Placeholder — extend with welcome / role-assignment logic.
    /// </summary>
    public static async Task GuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs args)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Fires when a member leaves (or is kicked/banned from) a guild.
    /// Placeholder — extend with audit / cleanup logic.
    /// </summary>
    public static async Task GuildMemberRemoved(DiscordClient sender, GuildMemberRemoveEventArgs args)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Dispatches a component interaction (button click, select menu, etc.)
    /// to the handler registered in <see cref="_interactionDispatchMap"/>. Any
    /// exception thrown by parsing or by the handler is caught here and
    /// logged in the same format as <c>SlashCommandErrored</c> in Program.cs
    /// so a bad component never silently breaks the bot.
    /// </summary>
    public static async Task ComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs args)
    {
        var customId = args.Interaction.Data.CustomId;
        await using var scope = _scopeFactory!.CreateAsyncScope();

        // Parse the custom_id → InteractionIdType. IEnumSerivce is scoped,
        // so we open a short-lived scope just for the lookup.
        InteractionIdType type;
        try
        {
            var enumService = scope.ServiceProvider.GetRequiredService<IEnumService>();
            type = enumService.ConvertInteraction(customId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.UtcNow.ToLocalTime()}] Component interaction '{customId}' failed to parse: {ex}");
            return;
        }

        // Look up the handler. The None entry in _interactionMap throws on
        // purpose for unrecognized ids — that throw is caught below.
        if (!_interactionDispatchMap.TryGetValue(type, out var handler))
        {
            Console.WriteLine($"[{DateTime.UtcNow.ToLocalTime()}] Component interaction '{customId}' has no registered handler for {type}.");
            return;
        }

        try
        {
            await handler(sender, args, scope.ServiceProvider);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.UtcNow.ToLocalTime()}] Component interaction '{customId}' errored: {ex}");
        }
    }

    /// <summary>
    /// Handler for <see cref="InteractionIdType.ClearAlert"/>: deletes the
    /// message the dismiss button was attached to. Doesn't use
    /// <paramref name="services"/> because no scoped service is needed —
    /// it just calls the Discord REST API.
    /// </summary>
    private static async Task ClearAlert(DiscordClient sender, ComponentInteractionCreateEventArgs args,
        IServiceProvider services)
    {
        await args.Message.DeleteAsync();
    }
}
