using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BusinessLogic.Enums.Types;

[JsonConverter(typeof(StringEnumConverter))]
public enum PrivacyOption
{
    Public,
    Private
}
