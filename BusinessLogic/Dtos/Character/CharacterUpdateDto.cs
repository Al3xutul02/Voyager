using BusinessLogic.Enums.Types;
using BusinessLogic.Json.Models;

namespace BusinessLogic.Dtos.Character;

/// <summary>
/// Data transfer object for updating an existing character.
/// </summary>
/// <param name="Id">The unique ID of the character.</param>
/// <param name="UserId">The Discord snowflake ID of the owning player.</param>
/// <param name="Name">The character's name.</param>
/// <param name="Description">Optional character biography or backstory.</param>
/// <param name="Color">Palette color used for the character's embeds.</param>
/// <param name="TokenImagePath">Relative path where the token image is stored.</param>
/// <param name="ActorImagePath">Relative path where the actor portrait image is stored.</param>
/// <param name="CurrentGame">The current game the character belongs to, or <c>null</c>.</param>
/// <param name="Gold">The character's total wealth in gold pieces (GP).</param>
/// <param name="PathbuilderId">Optional associated Pathbuilder export ID.</param>
/// <param name="Details">Character build details (ancestry, class, level, age, gender).</param>
public record CharacterUpdateDto(
    ulong Id,
    ulong UserId,
    string Name,
    string? Description,
    Color Color,
    string TokenImagePath,
    string ActorImagePath,
    string? CurrentGame,
    decimal Gold,
    string? PathbuilderId,
    CharacterDetails Details
);
