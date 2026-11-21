using BusinessLogic.Dtos.DiceRoll;
using Voyager.API.Commands.Autocompletes;

namespace VoyagerTests.Commands;

/// <summary>
/// Tests for <see cref="SavedDiceAutocompleteProvider"/> — verifies that saved
/// dice configurations are filtered case-insensitively by name or formula and
/// capped at Discord's 25-option autocomplete limit.
/// </summary>
[TestClass]
public class SavedDiceAutocompleteProviderTests
{
    [TestMethod]
    public void GetMatchingChoices_WhenInputNullOrEmpty_ReturnsUpTo25Choices()
    {
        var savedRolls = Enumerable.Range(1, 30)
            .Select(i => new DiceRollReadDto((ulong)i, 10, $"Roll{i}", $"1d{i + 1}"))
            .ToList();

        var choicesFromNull = SavedDiceAutocompleteProvider.GetMatchingChoices(savedRolls, null).ToList();
        var choicesFromEmpty = SavedDiceAutocompleteProvider.GetMatchingChoices(savedRolls, "   ").ToList();

        Assert.HasCount(25, choicesFromNull);
        Assert.HasCount(25, choicesFromEmpty);
    }

    [TestMethod]
    public void GetMatchingChoices_FiltersByNameOrFormulaCaseInsensitively()
    {
        var savedRolls = new List<DiceRollReadDto>
        {
            new(1, 10, "Fireball", "(6d6)[fire]"),
            new(2, 10, "Frost Ray", "(2d8)[cold]"),
            new(3, 10, "Greataxe", "1d12+4")
        };

        var byName = SavedDiceAutocompleteProvider.GetMatchingChoices(savedRolls, "fire").ToList();
        Assert.HasCount(1, byName);
        Assert.AreEqual("Fireball", byName[0].Value);
        Assert.AreEqual("Fireball ((6d6)[fire])", byName[0].Name);

        var byFormula = SavedDiceAutocompleteProvider.GetMatchingChoices(savedRolls, "1d12").ToList();
        Assert.HasCount(1, byFormula);
        Assert.AreEqual("Greataxe", byFormula[0].Value);
    }

    [TestMethod]
    public void GetMatchingChoices_WhenNoMatch_ReturnsEmpty()
    {
        var savedRolls = new List<DiceRollReadDto>
        {
            new(1, 10, "Fireball", "(6d6)[fire]")
        };

        var choices = SavedDiceAutocompleteProvider.GetMatchingChoices(savedRolls, "lightning").ToList();

        Assert.IsEmpty(choices);
    }
}
