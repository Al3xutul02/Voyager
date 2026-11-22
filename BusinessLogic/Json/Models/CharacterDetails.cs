namespace BusinessLogic.Json.Models;

/// <summary>
/// JSON model for character build details (ancestry, class, level, age, gender)
/// stored in the <c>Characters.Details</c> column.
/// </summary>
/// <param name="Ancestry">The character's ancestry (e.g. Human, Elf).</param>
/// <param name="Class">The character's class (e.g. Rogue, Fighter).</param>
/// <param name="Level">The character's level as a string (e.g. "3").</param>
/// <param name="Age">The character's age as a string (e.g. "25").</param>
/// <param name="Gender">The character's gender (e.g. Male, Female, Non-binary).</param>
public record CharacterDetails(
    string Ancestry = "",
    string Class = "",
    string Level = "",
    string Age = "",
    string Gender = ""
);
