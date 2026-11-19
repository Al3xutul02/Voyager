using BusinessLogic.Enums.Types;

namespace BusinessLogic.Json.Models;

/// <summary>
/// Server-wide configuration and UI color defaults stored in the
/// <c>Servers.Settings</c> JSON column and used by the view layer to color
/// generic, error, success, and warning notifications. Distinct from
/// <see cref="UserSettings"/>, which is per-user.
/// </summary>
/// <param name="Mode">The operating mode of the server (<see cref="ServerMode.Normal"/> or <see cref="ServerMode.Westmarch"/>).</param>
/// <param name="GeneralColor">Palette color used for general embeds and server profile cards.</param>
/// <param name="ErrorColor">Palette color used for error notifications.</param>
/// <param name="SuccessColor">Palette color used for success notifications.</param>
/// <param name="WarningColor">Palette color used for warning notifications.</param>
public record ServerSettings(
    ServerMode Mode = ServerMode.Normal,
    Color GeneralColor = Color.Teal,
    Color ErrorColor = Color.DarkRed,
    Color SuccessColor = Color.DarkGreen,
    Color WarningColor = Color.Orange);
