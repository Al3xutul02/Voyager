using BusinessLogic.Dtos.User;
using BusinessLogic.Enums.Types;
using BusinessLogic.Json.Models;
using BusinessLogic.Services;
using BusinessLogic.Services.Abstractions;
using NSubstitute;
using Voyager.API.Commands.Handlers;
using Voyager.API.Views;

namespace VoyagerTests.Commands;

/// <summary>
/// Tests for <see cref="ProfileHandler"/> — the testable core extracted from
/// the <c>/profile view</c> command. The user service is mocked; a real
/// <see cref="ViewFactory"/> (with a real <see cref="EnumService"/>) builds
/// the message so the rendered output is exercised too.
/// </summary>
[TestClass]
public class ProfileHandlerTests
{
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private ProfileHandler _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        var viewFactory = new ViewFactory(new EnumService(), new ServerSettings());
        _sut = new ProfileHandler(_userService, viewFactory);
    }

    [TestMethod]
    public async Task BuildProfileViewAsync_WhenUserExists_ReturnsProfileEmbed()
    {
        var dto = new UserReadDto(77, "Grace", new UserSettings(Color.Blue));
        _userService.GetByIdAsync(77).Returns(dto);

        var message = await _sut.BuildProfileViewAsync(77);

        Assert.HasCount(1, message.Embeds);
        Assert.AreEqual("Grace's Profile", message.Embeds[0].Title);
    }

    [TestMethod]
    public async Task BuildProfileViewAsync_WhenUserMissing_ReturnsErrorNotification()
    {
        _userService.GetByIdAsync(Arg.Any<ulong>()).Returns((UserReadDto?)null);

        var message = await _sut.BuildProfileViewAsync(999);

        Assert.HasCount(1, message.Embeds);
        Assert.AreEqual("Error: User not found.", message.Embeds[0].Title);
    }

    [TestMethod]
    public async Task BuildProfileViewAsync_QueriesTheGivenUserId()
    {
        _userService.GetByIdAsync(Arg.Any<ulong>()).Returns((UserReadDto?)null);

        await _sut.BuildProfileViewAsync(555);

        await _userService.Received(1).GetByIdAsync(555);
    }
}
