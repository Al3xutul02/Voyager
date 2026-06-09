using BusinessLogic.Enums.Types;

namespace BusinessLogic.Json.Models;

public record ServerSettings(
    Color GeneralColor = Color.Teal,
    Color ErrorColor = Color.DarkRed,
    Color SuccessColor = Color.DarkGreen);
