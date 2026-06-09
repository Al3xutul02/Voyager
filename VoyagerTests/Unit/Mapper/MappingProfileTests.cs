using AutoMapper;
using BusinessLogic.Dtos.User;
using BusinessLogic.Enums.Types;
using BusinessLogic.Json;
using BusinessLogic.Json.Models;
using Newtonsoft.Json;
using Repository.Models;
using VoyagerTests.TestHelpers;

namespace VoyagerTests.Mapper;

/// <summary>
/// Tests for the AutoMapper <c>MappingProfile</c>, focusing on the
/// User.Settings JSON round-trip and the positional-record ForCtorParam
/// mapping that previously regressed.
/// </summary>
[TestClass]
public class MappingProfileTests
{
    private readonly IMapper _mapper = MapperHelper.CreateMapper();

    [TestMethod]
    public void Configuration_IsValid()
    {
        // Catches unmapped destination members / broken expressions at the
        // earliest possible point.
        _mapper.ConfigurationProvider.AssertConfigurationIsValid();
    }

    [TestMethod]
    public void Map_UserToUserReadDto_DeserializesSettingsViaCtorParam()
    {
        // Arrange: a User row whose Settings column holds serialized JSON.
        var settingsJson = JsonConvert.SerializeObject(
            new UserSettings(Color.Red), VoyagerJsonSettings.Default);
        var user = new User { Id = 42, Name = "Ada", Settings = settingsJson };

        // Act
        var dto = _mapper.Map<UserReadDto>(user);

        // Assert: the JSON blob became a real UserSettings object — NOT the
        // raw string smuggled into Color (the old ForMember bug).
        Assert.AreEqual(42UL, dto.Id);
        Assert.AreEqual("Ada", dto.Name);
        Assert.AreEqual(Color.Red, dto.Settings.Color);
    }

    [TestMethod]
    public void Map_UserCreateDtoToUser_SerializesDefaultSettings()
    {
        var createDto = new UserCreateDto(7, "Grace");

        var user = _mapper.Map<User>(createDto);

        Assert.AreEqual(7UL, user.Id);
        Assert.AreEqual("Grace", user.Name);

        // Settings should be valid JSON deserializable back into UserSettings
        // with the default color.
        var settings = JsonConvert.DeserializeObject<UserSettings>(
            user.Settings, VoyagerJsonSettings.Default);
        Assert.IsNotNull(settings);
        Assert.AreEqual(new UserSettings().Color, settings!.Color);
    }

    [TestMethod]
    public void Map_UserUpdateDtoToUser_SerializesProvidedSettings()
    {
        var updateDto = new UserUpdateDto(9, "Linus", new UserSettings(Color.Green));

        var user = _mapper.Map<User>(updateDto);

        var settings = JsonConvert.DeserializeObject<UserSettings>(
            user.Settings, VoyagerJsonSettings.Default);
        Assert.IsNotNull(settings);
        Assert.AreEqual(Color.Green, settings!.Color);
    }

    [TestMethod]
    public void Map_UserToReadDtoAndBack_PreservesColor()
    {
        var original = new User
        {
            Id = 1,
            Name = "Round Trip",
            Settings = JsonConvert.SerializeObject(new UserSettings(Color.Magenta), VoyagerJsonSettings.Default)
        };

        var dto = _mapper.Map<UserReadDto>(original);
        var updateDto = new UserUpdateDto(dto.Id, dto.Name, dto.Settings);
        var roundTripped = _mapper.Map<User>(updateDto);

        var settings = JsonConvert.DeserializeObject<UserSettings>(
            roundTripped.Settings, VoyagerJsonSettings.Default);
        Assert.AreEqual(Color.Magenta, settings!.Color);
    }
}
