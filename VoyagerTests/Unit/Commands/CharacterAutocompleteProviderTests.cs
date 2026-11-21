using BusinessLogic.Dtos.Character;
using BusinessLogic.Enums.Types;
using BusinessLogic.Json.Models;
using Voyager.API.Commands.Autocompletes;

namespace VoyagerTests.Commands;

/// <summary>
/// Tests for <see cref="CharacterAutocompleteProvider"/> — verifies that characters
/// are filtered case-insensitively by name and formatted as "Name (Ancestry Class Level)".
/// </summary>
[TestClass]
public class CharacterAutocompleteProviderTests
{
    private static CharacterReadDto MakeChar(ulong id, string name, string ancestry = "Human", string cls = "Rogue", string level = "3") =>
        new(
            Id: id,
            UserId: 10,
            Name: name,
            Description: "Bio",
            Color: Color.Teal,
            TokenImagePath: "token.png",
            ActorImagePath: "actor.png",
            CurrentGame: null,
            Gold: 10m,
            PathbuilderId: "123456",
            Details: new CharacterDetails(ancestry, cls, level, "25", "Male")
        );

    [TestMethod]
    public void GetMatchingChoices_WhenInputNullOrEmpty_ReturnsUpTo10Choices()
    {
        var characters = Enumerable.Range(1, 15)
            .Select(i => MakeChar((ulong)i, $"Hero{i}"))
            .ToList();

        var choicesFromNull = CharacterAutocompleteProvider.GetMatchingChoices(characters, null).ToList();
        var choicesFromEmpty = CharacterAutocompleteProvider.GetMatchingChoices(characters, "   ").ToList();

        Assert.HasCount(10, choicesFromNull);
        Assert.HasCount(10, choicesFromEmpty);
    }

    [TestMethod]
    public void GetMatchingChoices_FiltersByNameCaseInsensitively()
    {
        var characters = new List<CharacterReadDto>
        {
            MakeChar(1, "Sunaki", "Human", "Rogue", "3"),
            MakeChar(2, "Valeros", "Human", "Fighter", "5"),
            MakeChar(3, "Kyra", "Human", "Cleric", "2")
        };

        var matches = CharacterAutocompleteProvider.GetMatchingChoices(characters, "sun").ToList();

        Assert.HasCount(1, matches);
        Assert.AreEqual("Sunaki", matches[0].Value);
        Assert.AreEqual("Sunaki (Human Rogue 3)", matches[0].Name);
    }

    [TestMethod]
    public void GetMatchingChoices_WhenNoMatch_ReturnsEmpty()
    {
        var characters = new List<CharacterReadDto>
        {
            MakeChar(1, "Sunaki")
        };

        var matches = CharacterAutocompleteProvider.GetMatchingChoices(characters, "Zorro").ToList();

        Assert.IsEmpty(matches);
    }
}
