using BusinessLogic.Enums.Types;

namespace BusinessLogic.Json.Models;

/// <summary>
/// JSON model for per-user application settings stored in the <c>Users.Settings</c> column.
/// </summary>
/// <param name="Color">Palette color used for the user's profile and settings embeds.</param>
/// <param name="GamePrivacy">Whether the user's game list is visible to other users.</param>
/// <param name="CharacterPrivacy">Whether the user's character list is visible to other users.</param>
public record UserSettings(
    Color Color = Color.Teal,
    PrivacyOption GamePrivacy = PrivacyOption.Public,
    PrivacyOption CharacterPrivacy = PrivacyOption.Public
);
