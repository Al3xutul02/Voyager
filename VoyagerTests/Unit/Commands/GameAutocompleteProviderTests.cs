using BusinessLogic.Dtos.Game;
using BusinessLogic.Enums.Types;
using Voyager.API.Commands.Autocompletes;

namespace VoyagerTests.Commands;

[TestClass]
public class GameAutocompleteProviderTests
{
    private static GameReadDto MakeGame(ulong id, string name) =>
        new(
            Id: id,
            ServerId: 100,
            GameMasterId: 10,
            GameMasterName: "GM",
            Name: name,
            Description: "Desc",
            Color: Color.Teal,
            GmTokenImagePath: "gm.png",
            ThumbnailImagePath: "thumb.png",
            CategoryId: 1,
            GmChannelId: 2,
            GeneralChannelId: 3,
            CharactersChannelId: 4,
            NpcsChannelId: 5,
            Players: []);

    [TestMethod]
    public void GetMatchingChoices_WhenInputEmpty_ReturnsUpToMaxGamesPerServer()
    {
        var games = Enumerable.Range(1, 7)
            .Select(i => MakeGame((ulong)i, $"Campaign {i}"))
            .ToList();

        var choices = GameAutocompleteProvider.GetMatchingChoices(games, null).ToList();

        Assert.HasCount(5, choices);
        Assert.AreEqual("Campaign 1", choices[0].Name);
        Assert.AreEqual("Campaign 1", choices[0].Value);
    }

    [TestMethod]
    public void GetMatchingChoices_FiltersCaseInsensitivelyByName()
    {
        var games = new List<GameReadDto>
        {
            MakeGame(1, "Abomination Vaults"),
            MakeGame(2, "Kingmaker"),
            MakeGame(3, "Strength of Thousands")
        };

        var choices = GameAutocompleteProvider.GetMatchingChoices(games, "vault").ToList();

        Assert.HasCount(1, choices);
        Assert.AreEqual("Abomination Vaults", choices[0].Name);
        Assert.AreEqual("Abomination Vaults", choices[0].Value);
    }
}
