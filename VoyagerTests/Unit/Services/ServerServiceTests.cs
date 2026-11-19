using AutoMapper;
using BusinessLogic.Dtos.Server;
using BusinessLogic.Enums.Types;
using BusinessLogic.Json;
using BusinessLogic.Json.Models;
using BusinessLogic.Services;
using Newtonsoft.Json;
using NSubstitute;
using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories.Abstractions;
using VoyagerTests.TestHelpers;

namespace VoyagerTests.Services;

/// <summary>
/// Tests for <see cref="ServerService"/> and the inherited <c>BaseService</c>
/// behavior. The repository is mocked with NSubstitute; a real AutoMapper
/// instance is used so mapping is exercised end to end.
/// </summary>
[TestClass]
public class ServerServiceTests
{
    private readonly IServerRepository _repository = Substitute.For<IServerRepository>();
    private readonly IMapper _mapper = MapperHelper.CreateMapper();
    private ServerService _sut = null!;

    private static Server MakeServer(
        ulong id = 1,
        string name = "TestServer",
        string? description = "A PbP server",
        ulong gmRoleId = 10,
        ulong adminChannelId = 20,
        ServerMode mode = ServerMode.Normal,
        Color generalColor = Color.Teal) =>
        new()
        {
            Id = id,
            Name = name,
            Description = description,
            GMRoleId = gmRoleId,
            AdminChannelId = adminChannelId,
            Settings = JsonConvert.SerializeObject(
                new ServerSettings(mode, generalColor),
                VoyagerJsonSettings.Default)
        };

    [TestInitialize]
    public void Setup() => _sut = new ServerService(_mapper, _repository);

    // ---- GetByName ------------------------------------------------------

    [TestMethod]
    public async Task GetByName_WhenServerExists_ReturnsMappedDto()
    {
        var server = MakeServer(10, "Golarion", "Westmarch campaign", 111, 222, ServerMode.Westmarch, Color.Gold);
        _repository.GetByNameAsync("Golarion", Arg.Any<IncludeBehavior>(), Arg.Any<Func<IQueryable<Server>, IQueryable<Server>>?>())
                   .Returns(server);

        var result = await _sut.GetByName("Golarion");

        Assert.IsNotNull(result);
        Assert.AreEqual(10UL, result!.Id);
        Assert.AreEqual("Golarion", result.Name);
        Assert.AreEqual("Westmarch campaign", result.Description);
        Assert.AreEqual(111UL, result.GMRoleId);
        Assert.AreEqual(222UL, result.AdminChannelId);
        Assert.AreEqual(ServerMode.Westmarch, result.Settings.Mode);
        Assert.AreEqual(Color.Gold, result.Settings.GeneralColor);
    }

    [TestMethod]
    public async Task GetByName_WhenServerMissing_ReturnsNull()
    {
        _repository.GetByNameAsync(Arg.Any<string>(), Arg.Any<IncludeBehavior>(), Arg.Any<Func<IQueryable<Server>, IQueryable<Server>>?>())
                   .Returns((Server?)null);

        var result = await _sut.GetByName("nonexistent");

        Assert.IsNull(result);
    }

    // ---- GetByIdAsync (BaseService) -------------------------------------

    [TestMethod]
    public async Task GetByIdAsync_WhenServerExists_ReturnsMappedDto()
    {
        var server = MakeServer(5, "Absalom");
        _repository.GetByIdAsync(5, Arg.Any<IncludeBehavior>(), Arg.Any<Func<IQueryable<Server>, IQueryable<Server>>?>())
                   .Returns(server);

        var result = await _sut.GetByIdAsync(5);

        Assert.IsNotNull(result);
        Assert.AreEqual(5UL, result!.Id);
        Assert.AreEqual("Absalom", result.Name);
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenServerMissing_ReturnsNull()
    {
        _repository.GetByIdAsync(Arg.Any<ulong>(), Arg.Any<IncludeBehavior>(), Arg.Any<Func<IQueryable<Server>, IQueryable<Server>>?>())
                   .Returns((Server?)null);

        var result = await _sut.GetByIdAsync(999);

        Assert.IsNull(result);
    }

    // ---- GetAllAsync ----------------------------------------------------

    [TestMethod]
    public async Task GetAllAsync_MapsEveryEntity()
    {
        var servers = new List<Server> { MakeServer(1, "S1"), MakeServer(2, "S2") };
        _repository.GetAllAsync(Arg.Any<IncludeBehavior>(), Arg.Any<Func<IQueryable<Server>, IQueryable<Server>>?>())
                   .Returns(servers);

        var result = (await _sut.GetAllAsync()).ToList();

        Assert.HasCount(2, result);
        CollectionAssert.AreEquivalent(new[] { "S1", "S2" }, result.Select(s => s.Name).ToList());
    }

    // ---- CreateAsync ----------------------------------------------------

    [TestMethod]
    public async Task CreateAsync_AddsMappedEntityAndSaves()
    {
        var createDto = new ServerCreateDto(3, "NewGuild", 100, 200, new ServerSettings(ServerMode.Westmarch));

        await _sut.CreateAsync(createDto);

        await _repository.Received(1).AddAsync(Arg.Is<Server>(s =>
            s.Id == 3 &&
            s.Name == "NewGuild" &&
            s.GMRoleId == 100 &&
            s.AdminChannelId == 200));
        await _repository.Received(1).SaveAsync();
    }

    // ---- UpdateAsync ----------------------------------------------------

    [TestMethod]
    public async Task UpdateAsync_UpdatesMappedEntityAndSaves()
    {
        var updateDto = new ServerUpdateDto(
            4,
            "EditedGuild",
            "Updated desc",
            300,
            400,
            new ServerSettings(ServerMode.Normal, Color.Blue));

        await _sut.UpdateAsync(updateDto);

        _repository.Received(1).Update(Arg.Is<Server>(s =>
            s.Id == 4 &&
            s.Name == "EditedGuild" &&
            s.Description == "Updated desc" &&
            s.GMRoleId == 300 &&
            s.AdminChannelId == 400));
        await _repository.Received(1).SaveAsync();
    }

    // ---- DeleteAsync ----------------------------------------------------

    [TestMethod]
    public async Task DeleteAsync_WhenServerExists_DeletesAndSaves()
    {
        var server = MakeServer(6, "ToDelete");
        _repository.GetByIdAsync(6, Arg.Any<IncludeBehavior>(), Arg.Any<Func<IQueryable<Server>, IQueryable<Server>>?>())
                   .Returns(server);

        await _sut.DeleteAsync(6);

        _repository.Received(1).Delete(server);
        await _repository.Received(1).SaveAsync();
    }

    [TestMethod]
    public async Task DeleteAsync_WhenServerMissing_Throws()
    {
        _repository.GetByIdAsync(Arg.Any<ulong>(), Arg.Any<IncludeBehavior>(), Arg.Any<Func<IQueryable<Server>, IQueryable<Server>>?>())
                   .Returns((Server?)null);

        await Assert.ThrowsAsync<Exception>(() => _sut.DeleteAsync(123));

        _repository.DidNotReceive().Delete(Arg.Any<Server>());
        await _repository.DidNotReceive().SaveAsync();
    }
}
