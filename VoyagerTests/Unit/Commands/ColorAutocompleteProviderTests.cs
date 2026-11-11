using Voyager.API.Commands.Autocompletes;

namespace VoyagerTests.Commands;

/// <summary>
/// Tests for <see cref="ColorAutocompleteProvider"/> — verifies that color
/// palette choices are filtered case-insensitively and capped at Discord's
/// 25-option autocomplete limit.
/// </summary>
[TestClass]
public class ColorAutocompleteProviderTests
{
    [TestMethod]
    public void GetMatchingChoices_WhenInputNullOrEmpty_ReturnsMax25Choices()
    {
        var choicesFromNull = ColorAutocompleteProvider.GetMatchingChoices(null).ToList();
        var choicesFromEmpty = ColorAutocompleteProvider.GetMatchingChoices("   ").ToList();

        Assert.HasCount(25, choicesFromNull);
        Assert.HasCount(25, choicesFromEmpty);
    }

    [TestMethod]
    public void GetMatchingChoices_FiltersCaseInsensitivelyBySubstring()
    {
        var choices = ColorAutocompleteProvider.GetMatchingChoices("blue")
            .Select(c => c.Name)
            .ToList();

        CollectionAssert.AreEquivalent(
            new[] { "Blue", "CornflowerBlue", "DarkBlue", "MidnightBlue", "PhthaloBlue" },
            choices);
    }

    [TestMethod]
    public void GetMatchingChoices_WhenNoMatch_ReturnsEmpty()
    {
        var choices = ColorAutocompleteProvider.GetMatchingChoices("notAColorAtAll").ToList();

        Assert.IsEmpty(choices);
    }
}
