using BusinessLogic.Enums.Types;
using BusinessLogic.Services;
using DSharpPlus.Entities;

namespace VoyagerTests.Services;

/// <summary>
/// Tests for <see cref="EnumService"/> — the bidirectional translation
/// between domain enums and their external representations.
/// </summary>
[TestClass]
public class EnumServiceTests
{
    private readonly EnumService _sut = new();

    // ---- Color: enum -> DiscordColor ------------------------------------

    [TestMethod]
    public void ConvertColor_FromPaletteColor_ReturnsMatchingDiscordColor()
    {
        Assert.AreEqual(DiscordColor.Gray, _sut.ConvertColor(Color.Gray));
        Assert.AreEqual(DiscordColor.Red, _sut.ConvertColor(Color.Red));
        Assert.AreEqual(DiscordColor.Blurple, _sut.ConvertColor(Color.Blurple));
    }

    [TestMethod]
    public void ConvertColor_EveryEnumValue_HasAPaletteEntry()
    {
        // Guards against adding a Color enum value but forgetting to register
        // it in EnumService.Palette (which would silently fall back to Teal).
        foreach (Color color in Enum.GetValues<Color>())
        {
            DiscordColor mapped = _sut.ConvertColor(color);

            // Teal is the fallback. Color.Teal legitimately maps to Teal, so
            // only the *other* values are suspicious if they resolve to Teal.
            if (color != Color.Teal)
            {
                Assert.AreNotEqual(
                    DiscordColor.Teal.Value,
                    mapped.Value,
                    $"Color.{color} has no palette entry and fell back to Teal.");
            }
        }
    }

    // ---- Color: DiscordColor -> enum ------------------------------------

    [TestMethod]
    public void ConvertColor_FromDiscordColor_ReturnsMatchingPaletteColor()
    {
        Assert.AreEqual(Color.Gray, _sut.ConvertColor(DiscordColor.Gray));
        Assert.AreEqual(Color.Red, _sut.ConvertColor(DiscordColor.Red));
    }

    [TestMethod]
    public void ConvertColor_FromUnknownDiscordColor_FallsBackToTeal()
    {
        // 0x123456 is an arbitrary value not present in the palette.
        var unknown = new DiscordColor(0x123456);
        Assert.AreEqual(Color.Teal, _sut.ConvertColor(unknown));
    }

    [TestMethod]
    public void ConvertColor_RoundTrips_ForEveryPaletteColor()
    {
        // enum -> DiscordColor -> enum should be the identity for every
        // registered color. This also fails loudly if two colors ever share
        // the same RGB value (the reverse lookup would then be ambiguous).
        foreach (Color color in Enum.GetValues<Color>())
        {
            DiscordColor discordColor = _sut.ConvertColor(color);
            Color roundTripped = _sut.ConvertColor(discordColor);

            Assert.AreEqual(color, roundTripped, $"Round-trip failed for Color.{color}.");
        }
    }

    // ---- Interaction id <-> string --------------------------------------

    [TestMethod]
    public void ConvertInteraction_KnownType_ReturnsRegisteredString()
    {
        Assert.AreEqual("clearAlert", _sut.ConvertInteraction(InteractionIdType.ClearAlert));
        Assert.AreEqual("none", _sut.ConvertInteraction(InteractionIdType.None));
    }

    [TestMethod]
    public void ConvertInteraction_KnownString_ReturnsType()
    {
        Assert.AreEqual(InteractionIdType.ClearAlert, _sut.ConvertInteraction("clearAlert"));
    }

    [TestMethod]
    public void ConvertInteraction_UnknownString_ReturnsNone()
    {
        Assert.AreEqual(InteractionIdType.None, _sut.ConvertInteraction("notARealCustomId"));
    }

    [TestMethod]
    public void ConvertInteraction_RoundTrips_ForEveryType()
    {
        foreach (InteractionIdType type in Enum.GetValues<InteractionIdType>())
        {
            string asString = _sut.ConvertInteraction(type);
            InteractionIdType roundTripped = _sut.ConvertInteraction(asString);

            Assert.AreEqual(type, roundTripped, $"Round-trip failed for {type}.");
        }
    }
}
