namespace Voyager.API.Enums.Types;

/// <summary>
/// Identifies the active view page on a user's own profile card so the
/// page-switcher dropdown can pre-select the currently displayed page.
/// </summary>
public enum ProfilePage
{
    /// <summary>The public/overview profile card showing games and characters.</summary>
    Profile,

    /// <summary>The user settings card showing customization and privacy options.</summary>
    Settings
}
