using AutoMapper;
using BusinessLogic.Dtos.Character;
using BusinessLogic.Enums.Types;
using BusinessLogic.Json.Models;
using BusinessLogic.Services;
using NSubstitute;
using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories.Abstractions;
using VoyagerTests.TestHelpers;

namespace VoyagerTests.Services;

/// <summary>
/// Unit tests for <see cref="CharacterService"/> covering CRUD operations,
/// filtering of soft-deleted characters, and repository delegation.
/// </summary>
[TestClass]
public class CharacterServiceTests
{
    private readonly ICharacterRepository _repository = Substitute.For<ICharacterRepository>();
    private readonly IMapper _mapper = MapperHelper.CreateMapper();
    private CharacterService _sut = null!;

    private static Character MakeCharacter(
        ulong id = 1,
        ulong userId = 10,
        string name = "Sunaki",
        DateTime? deletedAt = null) =>
        new()
        {
            Id = id,
            UserId = userId,
            Name = name,
            Description = "Rogue adventurer",
            Color = (int)Color.Teal,
            TokenImagePath = "Characters/10/1/token.png",
            ActorImagePath = "Characters/10/1/actor.png",
            CurrentGame = null,
            Gold = 5.0m,
            PathbuilderId = "123456",
            Details = "{\"Ancestry\":\"Human\",\"Class\":\"Rogue\",\"Level\":\"3\",\"Age\":\"25\",\"Gender\":\"Male\"}",
            DeletedAt = deletedAt
        };

    [TestInitialize]
    public void Setup() => _sut = new CharacterService(_mapper, _repository);

    [TestMethod]
    public async Task GetByUserIdAsync_ReturnsOnlyActiveCharacters()
    {
        var characters = new List<Character>
        {
            MakeCharacter(1, 10, "Sunaki"),
            MakeCharacter(2, 10, "OldChar", deletedAt: DateTime.UtcNow)
        };

        _repository.GetByUserIdAsync(10, IncludeBehavior.NoInclude)
            .Returns(characters);

        var results = (await _sut.GetByUserIdAsync(10)).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("Sunaki", results[0].Name);
        Assert.AreEqual(Color.Teal, results[0].Color);
    }

    [TestMethod]
    public async Task GetByUserAndNameAsync_ReturnsMappedDto_WhenActive()
    {
        var character = MakeCharacter(1, 10, "Sunaki");
        _repository.GetByUserAndNameAsync(10, "Sunaki", IncludeBehavior.NoInclude)
            .Returns(character);

        var result = await _sut.GetByUserAndNameAsync(10, "Sunaki");

        Assert.IsNotNull(result);
        Assert.AreEqual("Sunaki", result!.Name);
        Assert.AreEqual("Human", result.Details.Ancestry);
        Assert.AreEqual("Rogue", result.Details.Class);
    }

    [TestMethod]
    public async Task GetByUserAndNameAsync_ReturnsNull_WhenSoftDeleted()
    {
        var character = MakeCharacter(1, 10, "DeletedChar", deletedAt: DateTime.UtcNow);
        _repository.GetByUserAndNameAsync(10, "DeletedChar", IncludeBehavior.NoInclude)
            .Returns(character);

        var result = await _sut.GetByUserAndNameAsync(10, "DeletedChar");

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetByUserAndNameAsync_ReturnsNull_WhenNotFound()
    {
        _repository.GetByUserAndNameAsync(10, "Missing", IncludeBehavior.NoInclude)
            .Returns((Character?)null);

        var result = await _sut.GetByUserAndNameAsync(10, "Missing");

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetCountByUserIdAsync_DelegatesToRepository()
    {
        _repository.GetCountByUserIdAsync(10).Returns(3);

        var count = await _sut.GetCountByUserIdAsync(10);

        Assert.AreEqual(3, count);
        await _repository.Received(1).GetCountByUserIdAsync(10);
    }

    [TestMethod]
    public async Task CreateAsync_MapsAndAddsCharacter()
    {
        var createDto = new CharacterCreateDto(
            UserId: 10,
            Name: "Valeros",
            Description: "Fighter",
            Color: Color.Blue,
            TokenImagePath: "token.png",
            ActorImagePath: "actor.png",
            CurrentGame: null,
            Gold: 10m,
            PathbuilderId: "654321",
            Details: new CharacterDetails("Human", "Fighter", "1", "30", "Male")
        );

        await _sut.CreateAsync(createDto);

        await _repository.Received(1).AddAsync(Arg.Is<Character>(c =>
            c.UserId == 10 &&
            c.Name == "Valeros" &&
            c.Color == (int)Color.Blue));
        await _repository.Received(1).SaveAsync();
    }

    [TestMethod]
    public async Task UpdateAsync_MapsAndUpdatesCharacter()
    {
        var updateDto = new CharacterUpdateDto(
            Id: 1,
            UserId: 10,
            Name: "Sunaki Updated",
            Description: "Rogue",
            Color: Color.Gold,
            TokenImagePath: "token2.png",
            ActorImagePath: "actor2.png",
            CurrentGame: "Campaign",
            Gold: 20m,
            PathbuilderId: "123456",
            Details: new CharacterDetails("Human", "Rogue", "4", "26", "Male")
        );

        await _sut.UpdateAsync(updateDto);

        _repository.Received(1).Update(Arg.Is<Character>(c =>
            c.Id == 1 &&
            c.Name == "Sunaki Updated" &&
            c.Gold == 20m));
        await _repository.Received(1).SaveAsync();
    }

    [TestMethod]
    public async Task DeleteAsync_SetsDeletedAtAndCallsUpdate()
    {
        var character = MakeCharacter(1, 10, "Sunaki");
        _repository.GetByIdAsync(1, IncludeBehavior.NoInclude).Returns(character);

        await _sut.DeleteAsync(1);

        Assert.IsNotNull(character.DeletedAt);
        _repository.Received(1).Update(character);
        await _repository.SaveAsync();
    }

    [TestMethod]
    public async Task DeleteAsync_ThrowsKeyNotFoundException_WhenMissing()
    {
        _repository.GetByIdAsync(999, IncludeBehavior.NoInclude).Returns((Character?)null);

        bool threw = false;
        try
        {
            await _sut.DeleteAsync(999);
        }
        catch (KeyNotFoundException)
        {
            threw = true;
        }

        Assert.IsTrue(threw);
    }
}
