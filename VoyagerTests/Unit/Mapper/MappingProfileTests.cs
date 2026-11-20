using AutoMapper;
using BusinessLogic.Dtos.DiceRoll;
using BusinessLogic.Dtos.Server;
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
/// User.Settings and Server.Settings JSON round-trip and the positional-record
/// ForCtorParam mapping that previously regressed.
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

    // ---- User mappings --------------------------------------------------

    [TestMethod]
    public void Map_UserToUserReadDto_DeserializesSettingsViaCtorParam()
    {
        // Arrange: a User row whose Settings column holds serialized JSON.
        var settingsJson = JsonConvert.SerializeObject(
            new UserSettings(Color.Red, PrivacyOption.Private, PrivacyOption.Public),
            VoyagerJsonSettings.Default);
        var user = new User { Id = 42, Name = "Ada", Settings = settingsJson };

        // Act
        var dto = _mapper.Map<UserReadDto>(user);

        // Assert: the JSON blob became a real UserSettings object — NOT the
        // raw string smuggled into Color (the old ForMember bug).
        Assert.AreEqual(42UL, dto.Id);
        Assert.AreEqual("Ada", dto.Name);
        Assert.AreEqual(Color.Red, dto.Settings.Color);
        Assert.AreEqual(PrivacyOption.Private, dto.Settings.GamePrivacy);
        Assert.AreEqual(PrivacyOption.Public, dto.Settings.CharacterPrivacy);
    }

    [TestMethod]
    public void Map_UserCreateDtoToUser_SerializesDefaultSettings()
    {
        var createDto = new UserCreateDto(7, "Grace", new UserSettings());

        var user = _mapper.Map<User>(createDto);

        Assert.AreEqual(7UL, user.Id);
        Assert.AreEqual("Grace", user.Name);

        // Settings should be valid JSON deserializable back into UserSettings
        // with the default color and privacy options.
        var settings = JsonConvert.DeserializeObject<UserSettings>(
            user.Settings, VoyagerJsonSettings.Default);
        Assert.IsNotNull(settings);
        Assert.AreEqual(new UserSettings().Color, settings!.Color);
        Assert.AreEqual(PrivacyOption.Public, settings.GamePrivacy);
        Assert.AreEqual(PrivacyOption.Public, settings.CharacterPrivacy);
    }

    [TestMethod]
    public void Map_UserUpdateDtoToUser_SerializesProvidedSettings()
    {
        var updateDto = new UserUpdateDto(
            9,
            "Linus",
            new UserSettings(Color.Green, PrivacyOption.Private, PrivacyOption.Private));

        var user = _mapper.Map<User>(updateDto);

        var settings = JsonConvert.DeserializeObject<UserSettings>(
            user.Settings, VoyagerJsonSettings.Default);
        Assert.IsNotNull(settings);
        Assert.AreEqual(Color.Green, settings!.Color);
        Assert.AreEqual(PrivacyOption.Private, settings.GamePrivacy);
        Assert.AreEqual(PrivacyOption.Private, settings.CharacterPrivacy);
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

    // ---- Server mappings ------------------------------------------------

    [TestMethod]
    public void Map_ServerToServerReadDto_DeserializesSettingsViaCtorParam()
    {
        var settingsJson = JsonConvert.SerializeObject(
            new ServerSettings(ServerMode.Westmarch, Color.Gold, Color.Red, Color.Green, Color.Yellow),
            VoyagerJsonSettings.Default);
        var server = new Server
        {
            Id = 100,
            Name = "Pathfinder Hub",
            Description = "A shared Westmarch world",
            GMRoleId = 200,
            AdminChannelId = 300,
            Settings = settingsJson
        };

        var dto = _mapper.Map<ServerReadDto>(server);

        Assert.AreEqual(100UL, dto.Id);
        Assert.AreEqual("Pathfinder Hub", dto.Name);
        Assert.AreEqual("A shared Westmarch world", dto.Description);
        Assert.AreEqual(200UL, dto.GMRoleId);
        Assert.AreEqual(300UL, dto.AdminChannelId);
        Assert.AreEqual(ServerMode.Westmarch, dto.Settings.Mode);
        Assert.AreEqual(Color.Gold, dto.Settings.GeneralColor);
        Assert.AreEqual(Color.Red, dto.Settings.ErrorColor);
        Assert.AreEqual(Color.Green, dto.Settings.SuccessColor);
        Assert.AreEqual(Color.Yellow, dto.Settings.WarningColor);
    }

    [TestMethod]
    public void Map_ServerCreateDtoToServer_SerializesSettings()
    {
        var createDto = new ServerCreateDto(
            10,
            "NewServer",
            111,
            222,
            new ServerSettings(ServerMode.Westmarch));

        var server = _mapper.Map<Server>(createDto);

        Assert.AreEqual(10UL, server.Id);
        Assert.AreEqual("NewServer", server.Name);
        Assert.IsNull(server.Description);
        Assert.AreEqual(111UL, server.GMRoleId);
        Assert.AreEqual(222UL, server.AdminChannelId);

        var settings = JsonConvert.DeserializeObject<ServerSettings>(
            server.Settings, VoyagerJsonSettings.Default);
        Assert.IsNotNull(settings);
        Assert.AreEqual(ServerMode.Westmarch, settings!.Mode);
        Assert.AreEqual(Color.Teal, settings.GeneralColor);
    }

    [TestMethod]
    public void Map_ServerUpdateDtoToServer_SerializesProvidedSettingsAndDescription()
    {
        var updateDto = new ServerUpdateDto(
            10,
            "UpdatedServer",
            "Custom description",
            333,
            444,
            new ServerSettings(ServerMode.Normal, Color.Purple, Color.DarkRed, Color.SpringGreen, Color.Orange));

        var server = _mapper.Map<Server>(updateDto);

        Assert.AreEqual(10UL, server.Id);
        Assert.AreEqual("UpdatedServer", server.Name);
        Assert.AreEqual("Custom description", server.Description);
        Assert.AreEqual(333UL, server.GMRoleId);
        Assert.AreEqual(444UL, server.AdminChannelId);

        var settings = JsonConvert.DeserializeObject<ServerSettings>(
            server.Settings, VoyagerJsonSettings.Default);
        Assert.IsNotNull(settings);
        Assert.AreEqual(ServerMode.Normal, settings!.Mode);
        Assert.AreEqual(Color.Purple, settings.GeneralColor);
        Assert.AreEqual(Color.SpringGreen, settings.SuccessColor);
    }

    // ---- DiceRoll mappings ----------------------------------------------

    [TestMethod]
    public void Map_DiceRollCreateDtoToDiceRoll_MapsUserIdNameAndFormula()
    {
        var createDto = new DiceRollCreateDto(42, "Fireball", "(6d6)[fire]");

        var entity = _mapper.Map<DiceRoll>(createDto);

        Assert.AreEqual(42UL, entity.UserId);
        Assert.AreEqual("Fireball", entity.Name);
        Assert.AreEqual("(6d6)[fire]", entity.Formula);
    }

    [TestMethod]
    public void Map_DiceRollToDiceRollReadDto_MapsAllProperties()
    {
        var entity = new DiceRoll { Id = 99, UserId = 42, Name = "Longsword", Formula = "1d8+4" };

        var dto = _mapper.Map<DiceRollReadDto>(entity);

        Assert.AreEqual(99UL, dto.Id);
        Assert.AreEqual(42UL, dto.UserId);
        Assert.AreEqual("Longsword", dto.Name);
        Assert.AreEqual("1d8+4", dto.Formula);
    }

    [TestMethod]
    public void Map_DiceRollUpdateDtoToDiceRoll_MapsAllProperties()
    {
        var updateDto = new DiceRollUpdateDto(99, 42, "Longsword +1", "2d8+4");

        var entity = _mapper.Map<DiceRoll>(updateDto);

        Assert.AreEqual(99UL, entity.Id);
        Assert.AreEqual(42UL, entity.UserId);
        Assert.AreEqual("Longsword +1", entity.Name);
        Assert.AreEqual("2d8+4", entity.Formula);
    }
}
