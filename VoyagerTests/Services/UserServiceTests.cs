using AutoMapper;
using BusinessLogic.Dtos.User;
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
/// Tests for <see cref="UserService"/> and the inherited <c>BaseService</c>
/// behavior. The repository is mocked with NSubstitute; a real AutoMapper
/// instance is used so mapping is exercised end to end.
/// </summary>
[TestClass]
public class UserServiceTests
{
    private readonly IUserRepository _repository = Substitute.For<IUserRepository>();
    private readonly IMapper _mapper = MapperHelper.CreateMapper();
    private UserService _sut = null!;

    private static User MakeUser(ulong id = 1, string name = "Tester", Color color = Color.Teal) =>
        new()
        {
            Id = id,
            Name = name,
            Settings = JsonConvert.SerializeObject(new UserSettings(color), VoyagerJsonSettings.Default)
        };

    [TestInitialize]
    public void Setup() => _sut = new UserService(_mapper, _repository);

    // ---- GetByName ------------------------------------------------------

    [TestMethod]
    public async Task GetByName_WhenUserExists_ReturnsMappedDto()
    {
        var user = MakeUser(10, "Ada", Color.Red);
        _repository.GetByNameAsync("Ada", Arg.Any<IncludeBehavior>(), Arg.Any<Func<IQueryable<User>, IQueryable<User>>?>())
                   .Returns(user);

        var result = await _sut.GetByName("Ada");

        Assert.IsNotNull(result);
        Assert.AreEqual(10UL, result!.Id);
        Assert.AreEqual("Ada", result.Name);
        Assert.AreEqual(Color.Red, result.Settings.Color);
    }

    [TestMethod]
    public async Task GetByName_WhenUserMissing_ReturnsNull()
    {
        _repository.GetByNameAsync(Arg.Any<string>(), Arg.Any<IncludeBehavior>(), Arg.Any<Func<IQueryable<User>, IQueryable<User>>?>())
                   .Returns((User?)null);

        var result = await _sut.GetByName("ghost");

        Assert.IsNull(result);
    }

    // ---- GetByIdAsync (BaseService) -------------------------------------

    [TestMethod]
    public async Task GetByIdAsync_WhenUserExists_ReturnsMappedDto()
    {
        var user = MakeUser(5, "Grace");
        _repository.GetByIdAsync(5, Arg.Any<IncludeBehavior>(), Arg.Any<Func<IQueryable<User>, IQueryable<User>>?>())
                   .Returns(user);

        var result = await _sut.GetByIdAsync(5);

        Assert.IsNotNull(result);
        Assert.AreEqual(5UL, result!.Id);
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenUserMissing_ReturnsNull()
    {
        // Regression guard: the nullable return type must mean "null when not
        // found", not "throws" — otherwise the /profile not-found path hangs.
        _repository.GetByIdAsync(Arg.Any<ulong>(), Arg.Any<IncludeBehavior>(), Arg.Any<Func<IQueryable<User>, IQueryable<User>>?>())
                   .Returns((User?)null);

        var result = await _sut.GetByIdAsync(999);

        Assert.IsNull(result);
    }

    // ---- GetAllAsync ----------------------------------------------------

    [TestMethod]
    public async Task GetAllAsync_MapsEveryEntity()
    {
        var users = new List<User> { MakeUser(1, "A"), MakeUser(2, "B") };
        _repository.GetAllAsync(Arg.Any<IncludeBehavior>(), Arg.Any<Func<IQueryable<User>, IQueryable<User>>?>())
                   .Returns(users);

        var result = (await _sut.GetAllAsync()).ToList();

        Assert.HasCount(2, result);
        CollectionAssert.AreEquivalent(new[] { "A", "B" }, result.Select(u => u.Name).ToList());
    }

    // ---- CreateAsync ----------------------------------------------------

    [TestMethod]
    public async Task CreateAsync_AddsMappedEntityAndSaves()
    {
        var createDto = new UserCreateDto(3, "New");

        await _sut.CreateAsync(createDto);

        await _repository.Received(1).AddAsync(Arg.Is<User>(u => u.Id == 3 && u.Name == "New"));
        await _repository.Received(1).SaveAsync();
    }

    // ---- UpdateAsync ----------------------------------------------------

    [TestMethod]
    public async Task UpdateAsync_UpdatesMappedEntityAndSaves()
    {
        var updateDto = new UserUpdateDto(4, "Edited", new UserSettings(Color.Blue));

        await _sut.UpdateAsync(updateDto);

        _repository.Received(1).Update(Arg.Is<User>(u => u.Id == 4 && u.Name == "Edited"));
        await _repository.Received(1).SaveAsync();
    }

    // ---- DeleteAsync ----------------------------------------------------

    [TestMethod]
    public async Task DeleteAsync_WhenUserExists_DeletesAndSaves()
    {
        var user = MakeUser(6, "ToDelete");
        _repository.GetByIdAsync(6, Arg.Any<IncludeBehavior>(), Arg.Any<Func<IQueryable<User>, IQueryable<User>>?>())
                   .Returns(user);

        await _sut.DeleteAsync(6);

        _repository.Received(1).Delete(user);
        await _repository.Received(1).SaveAsync();
    }

    [TestMethod]
    public async Task DeleteAsync_WhenUserMissing_Throws()
    {
        _repository.GetByIdAsync(Arg.Any<ulong>(), Arg.Any<IncludeBehavior>(), Arg.Any<Func<IQueryable<User>, IQueryable<User>>?>())
                   .Returns((User?)null);

        await Assert.ThrowsAsync<Exception>(() => _sut.DeleteAsync(123));

        _repository.DidNotReceive().Delete(Arg.Any<User>());
        await _repository.DidNotReceive().SaveAsync();
    }
}
