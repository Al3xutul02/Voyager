namespace BusinessLogic.Enums.Types;

/// <summary>
/// Identifies the meaning of a component interaction's <c>custom_id</c>.
/// New buttons / select menus / etc. should add a value here and register
/// the corresponding string mapping in <c>EnumService.InteractionMap</c>
/// plus a handler in <c>EventHandler._interactionDispatchMap</c>.
/// </summary>
public enum InteractionIdType
{
    /// <summary>
    /// Sentinel for an unknown or unregistered <c>custom_id</c>. The
    /// dispatcher's <c>None</c> handler throws so the event handler can
    /// log it as an unrecognized component.
    /// </summary>
    None,

    /// <summary>
    /// Dismiss-button on transient notifications/alerts — the handler
    /// deletes the message the button is attached to.
    /// </summary>
    ClearAlert
}
