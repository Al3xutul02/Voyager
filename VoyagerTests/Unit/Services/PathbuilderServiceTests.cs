using System.Net;
using BusinessLogic.Services;

namespace VoyagerTests.Services;

/// <summary>
/// Unit tests for <see cref="PathbuilderService"/>.
/// Validates ID sanitization, JSON parsing, field mapping, and gold calculations.
/// </summary>
[TestClass]
public class PathbuilderServiceTests
{
    [TestMethod]
    public void SanitizeExportId_HandlesRawNumericId()
    {
        using var client = new HttpClient();
        var service = new PathbuilderService(client);

        Assert.AreEqual("123456", service.SanitizeExportId("123456"));
        Assert.AreEqual("123456", service.SanitizeExportId("  123456  "));
    }

    [TestMethod]
    public void SanitizeExportId_HandlesFullUrl()
    {
        using var client = new HttpClient();
        var service = new PathbuilderService(client);

        Assert.AreEqual("987654", service.SanitizeExportId("https://pathbuilder2e.com/json.php?id=987654"));
        Assert.AreEqual("987654", service.SanitizeExportId("https://pathbuilder2e.com/json.php?id=987654&view=full"));
    }

    [TestMethod]
    public void SanitizeExportId_WhenEmpty_ReturnsEmpty()
    {
        using var client = new HttpClient();
        var service = new PathbuilderService(client);

        Assert.AreEqual(string.Empty, service.SanitizeExportId(string.Empty));
        Assert.AreEqual(string.Empty, service.SanitizeExportId("   "));
    }

    [TestMethod]
    public async Task FetchCharacterAsync_WhenValidJson_ParsesAllFieldsAndCalculatesGold()
    {
        var sampleJson = """
        {
            "success": true,
            "build": {
                "name": "Sunaki",
                "class": "Rogue",
                "level": 3,
                "ancestry": "Human",
                "gender": "Male",
                "age": "25",
                "money": {
                    "pp": 0,
                    "gp": 0,
                    "sp": 3,
                    "cp": 0
                }
            }
        }
        """;

        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(sampleJson)
        });

        using var client = new HttpClient(handler);
        var service = new PathbuilderService(client);

        var character = await service.FetchCharacterAsync("123456");

        Assert.IsNotNull(character);
        Assert.AreEqual("Sunaki", character!.Name);
        Assert.AreEqual("Human", character.Ancestry);
        Assert.AreEqual("Rogue", character.Class);
        Assert.AreEqual("3", character.Level);
        Assert.AreEqual("25", character.Age);
        Assert.AreEqual("Male", character.Gender);
        Assert.AreEqual(0.3m, character.Gold);
        Assert.AreEqual("123456", character.PathbuilderId);
    }

    [TestMethod]
    public async Task FetchCharacterAsync_CalculatesGoldCorrectly_WithAllDenominations()
    {
        var sampleJson = """
        {
            "success": true,
            "build": {
                "name": "Valeros",
                "class": "Fighter",
                "level": 5,
                "ancestry": "Human",
                "gender": "Male",
                "age": "30",
                "money": {
                    "pp": 2,
                    "gp": 15,
                    "sp": 4,
                    "cp": 10
                }
            }
        }
        """;

        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(sampleJson)
        });

        using var client = new HttpClient(handler);
        var service = new PathbuilderService(client);

        var character = await service.FetchCharacterAsync("222222");

        Assert.IsNotNull(character);
        // 2 pp * 10 = 20, 15 gp = 15, 4 sp * 0.1 = 0.4, 10 cp * 0.01 = 0.10 => 35.50
        Assert.AreEqual(35.50m, character!.Gold);
    }

    [TestMethod]
    public async Task FetchCharacterAsync_WhenSuccessFalse_ReturnsNull()
    {
        var sampleJson = """
        {
            "success": false
        }
        """;

        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(sampleJson)
        });

        using var client = new HttpClient(handler);
        var service = new PathbuilderService(client);

        var character = await service.FetchCharacterAsync("invalid_id");

        Assert.IsNull(character);
    }

    [TestMethod]
    public async Task FetchCharacterAsync_WhenHttpError_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));

        using var client = new HttpClient(handler);
        var service = new PathbuilderService(client);

        var character = await service.FetchCharacterAsync("404_id");

        Assert.IsNull(character);
    }

    [TestMethod]
    public async Task FetchCharacterAsync_WhenInvalidJson_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<!DOCTYPE html><html>Not JSON</html>")
        });

        using var client = new HttpClient(handler);
        var service = new PathbuilderService(client);

        var character = await service.FetchCharacterAsync("bad_json");

        Assert.IsNull(character);
    }

    private class MockHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(response);
    }
}
