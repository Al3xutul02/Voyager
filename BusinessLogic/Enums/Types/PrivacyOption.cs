using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BusinessLogic.Enums.Types;

/// <summary>
/// Controls visibility of a section on a user's profile card when viewed by
/// other users. Serialized as its string name in JSON settings columns.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum PrivacyOption
{
    /// <summary>Visible to anyone viewing the user's profile.</summary>
    Public,

    /// <summary>Visible only when the user views their own profile.</summary>
    Private
}
