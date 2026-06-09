using BusinessLogic.Enums.Types;

namespace BusinessLogic.Json.Models;

/// <summary>
/// Server-wide UI defaults used by the view layer to color generic,
/// error, and success notifications. Distinct from <see cref="UserSettings"/>,
/// which is per-user.
/// </summary>
public record ServerSettings(
    Color GeneralColor = Color.Teal,
    Color ErrorColor = Color.DarkRed,
    Color SuccessColor = Color.DarkGreen);
