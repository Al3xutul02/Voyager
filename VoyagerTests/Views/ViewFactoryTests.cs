using BusinessLogic.Dtos.User;
using BusinessLogic.Enums.Types;
using BusinessLogic.Json.Models;
using BusinessLogic.Services;
using DSharpPlus.Entities;
using Voyager.API.Enums.Types;
using Voyager.API.Views;

namespace VoyagerTests.Views;

/// <summary>
/// Tests for <see cref="ViewFactory"/>.
/// </summary>
[TestClass]
public class ViewFactoryTests
{
    private readonly ServerSettings _settings = new();
    private ViewFactory _sut = null!;

    [TestInitialize]
    public void Setup() => _sut = new ViewFactory(new EnumService(), _settings);

    [TestMethod]
    public void CreateNotification_Error_BuildsEmbedWithErrorColorAndButton()
    {
        var message = _sut.CreateNotification(NotificationType.Error, "Something broke");

        Assert.HasCount(1, message.Embeds);
        DiscordEmbed embed = message.Embeds[0];
        Assert.AreEqual("Something broke", embed.Title);
        Assert.AreEqual(new EnumService().ConvertColor(_settings.ErrorColor).Value, embed.Color.Value.Value);

        // A clear button row is attached.
        Assert.HasCount(1, message.Components);
    }

    [TestMethod]
    public void CreateNotification_Success_UsesSuccessColor()
    {
        var message = _sut.CreateNotification(NotificationType.Success, "Done");

        Assert.AreEqual(
            new EnumService().ConvertColor(_settings.SuccessColor).Value,
            message.Embeds[0].Color.Value.Value);
    }

    [TestMethod]
    public void CreateUserProfile_BuildsProfileEmbedWithUserColor()
    {
        var dto = new UserReadDto(123, "Ada", new UserSettings(Color.Red));

        var message = _sut.CreateUserProfile(dto);

        Assert.HasCount(1, message.Embeds);
        DiscordEmbed embed = message.Embeds[0];
        Assert.AreEqual("Ada's Profile", embed.Title);
        Assert.AreEqual(DiscordColor.Red.Value, embed.Color.Value.Value);
        Assert.HasCount(1, message.Components);
    }
}
