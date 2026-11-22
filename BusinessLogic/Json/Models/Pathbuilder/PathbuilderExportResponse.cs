namespace BusinessLogic.Json.Models.Pathbuilder;

/// <summary>
/// Root model deserialized from Pathbuilder's export API (<c>https://pathbuilder2e.com/json.php?id={id}</c>).
/// </summary>
public class PathbuilderExportResponse
{
    /// <summary>
    /// Indicates whether the export ID was valid and fetched successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Character build details when <see cref="Success"/> is <c>true</c>.
    /// </summary>
    public PathbuilderBuild? Build { get; set; }
}

/// <summary>
/// Character build fields within Pathbuilder's export payload.
/// </summary>
public class PathbuilderBuild
{
    public string? Name { get; set; }
    public string? Class { get; set; }
    public string? Ancestry { get; set; }
    public object? Level { get; set; }
    public object? Age { get; set; }
    public string? Gender { get; set; }
    public PathbuilderMoney? Money { get; set; }
}

/// <summary>
/// Currency breakdown in Pathbuilder's export payload.
/// </summary>
public class PathbuilderMoney
{
    public decimal Pp { get; set; }
    public decimal Gp { get; set; }
    public decimal Sp { get; set; }
    public decimal Cp { get; set; }
}
