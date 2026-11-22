namespace BusinessLogic.Dtos.Pathbuilder;

/// <summary>
/// Data transfer object holding extracted character information from a Pathbuilder 2e export.
/// </summary>
public record PathbuilderCharacterDto(
    string Name,
    string Ancestry,
    string Class,
    string Level,
    string Age,
    string Gender,
    decimal Gold,
    string? PathbuilderId = null
);
