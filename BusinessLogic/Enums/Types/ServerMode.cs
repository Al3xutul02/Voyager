using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BusinessLogic.Enums.Types;

/// <summary>
/// Defines the operating mode of a Discord server using Voyager, which
/// governs channel structures, permissions, and game organization.
/// Serialized as its string name in JSON settings columns.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ServerMode
{
    /// <summary>
    /// Supports multiple concurrent, isolated games on the same Discord server.
    /// </summary>
    Normal,

    /// <summary>
    /// A shared-world server environment with interconnected channels and shared mission boards.
    /// </summary>
    Westmarch
}
