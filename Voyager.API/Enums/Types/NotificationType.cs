namespace Voyager.API.Enums.Types;

/// <summary>
/// Categorizes the visual style of a notification embed. Selects which
/// <c>ServerSettings</c> color is used when rendering the embed.
/// </summary>
public enum NotificationType
{
    /// <summary>Neutral / informational notification.</summary>
    General,

    /// <summary>Something went wrong — rendered with the error color.</summary>
    Error,

    /// <summary>Action completed successfully — rendered with the success color.</summary>
    Success
}
