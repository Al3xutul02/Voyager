using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BusinessLogic.Json;

/// <summary>
/// Project-wide Newtonsoft.Json settings. Pass these explicitly to
/// <see cref="JsonConvert.SerializeObject(object?, JsonSerializerSettings)"/>
/// / <see cref="JsonConvert.DeserializeObject{T}(string, JsonSerializerSettings)"/>
/// for any JSON your own code owns (e.g. the <c>UserSettings</c> blob persisted
/// on <c>User.Settings</c>).
///
/// <para>
/// IMPORTANT: do <b>not</b> install these as <see cref="JsonConvert.DefaultSettings"/>.
/// Third-party libraries (DSharpPlus, etc.) use Newtonsoft internally with
/// their own <c>[JsonProperty]</c> attributes, and a global
/// <see cref="CamelCasePropertyNamesContractResolver"/> will collide with
/// those attributes — e.g. throwing "A member with the name 'components'
/// already exists on DiscordActionRowComponent" the first time a component
/// is serialized. Always pass these settings explicitly.
/// </para>
/// </summary>
public static class VoyagerJsonSettings
{
    public static readonly JsonSerializerSettings Default = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Include,
        DefaultValueHandling = DefaultValueHandling.Populate,
        DateFormatHandling = DateFormatHandling.IsoDateFormat,
        ReferenceLoopHandling = ReferenceLoopHandling.Error,
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
    };
}
