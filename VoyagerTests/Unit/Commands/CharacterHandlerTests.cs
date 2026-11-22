using System.IO;
using System.Text;
using BusinessLogic.Dtos.Character;
using BusinessLogic.Dtos.Pathbuilder;
using BusinessLogic.Dtos.User;
using BusinessLogic.Enums.Types;
using BusinessLogic.Json.Models;
using BusinessLogic.Services;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Voyager.API.Commands.Handlers;
using Voyager.API.Events;
using Voyager.API.Views;

namespace VoyagerTests.Commands;

[TestClass]
public class CharacterHandlerTests
{
    private readonly ICharacterService _characterService = Substitute.For<ICharacterService>();
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly IPathbuilderService _pathbuilderService = Substitute.For<IPathbuilderService>();
    private readonly IFileService _fileService = Substitute.For<IFileService>();
    private readonly IEnumService _enumService = new EnumService();
    private CharacterHandler _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        var viewFactory = new ViewFactory(false, _enumService, new ServerSettings());
        _sut = new CharacterHandler(
            _characterService,
            _userService,
            _pathbuilderService,
            _fileService,
            _enumService,
            viewFactory);
    }

    private static PathbuilderCharacterDto CreatePbDto(string name = "Sunaki") =>
        new(name, "Human", "Rogue", "3", "25", "Male", 10.5m, "12345");

    private static CharacterReadDto CreateCharDto(
        ulong id = 1,
        ulong userId = 42,
        string name = "Sunaki",
        string? currentGame = null) =>
        new(
            id,
            userId,
            name,
            "A stealthy rogue.",
            Color.Teal,
            $"characters/{userId}/{name}/token.png",
            $"characters/{userId}/{name}/actor.png",
            currentGame,
            10.5m,
            "12345",
            new CharacterDetails("Human", "Rogue", "3", "25", "Male"));

    // ---- CreateAsync ----------------------------------------------------

    [TestMethod]
    public async Task CreateAsync_WhenUserDoesNotExist_ReturnsError()
    {
        _userService.GetByIdAsync(42).Returns((UserReadDto?)null);

        var message = await _sut.CreateAsync(
            callerId: 42,
            callerUsername: "Tester",
            color: "Blue",
            pathbuilderId: "12345",
            tokenUrl: "http://example.com/token.png",
            tokenFileName: "token.png",
            actorUrl: "http://example.com/actor.png",
            actorFileName: "actor.png");

        Assert.HasCount(1, message.Embeds);
        StringAssert.Contains(message.Embeds[0].Title, "create a profile first");
    }

    [TestMethod]
    public async Task CreateAsync_WhenUserHas10Characters_ReturnsError()
    {
        _userService.GetByIdAsync(42).Returns(new UserReadDto(42, "Tester", new UserSettings()));
        _characterService.GetCountByUserIdAsync(42).Returns(10);

        var message = await _sut.CreateAsync(
            callerId: 42,
            callerUsername: "Tester",
            color: "Blue",
            pathbuilderId: "12345",
            tokenUrl: "http://example.com/token.png",
            tokenFileName: "token.png",
            actorUrl: "http://example.com/actor.png",
            actorFileName: "actor.png");

        Assert.HasCount(1, message.Embeds);
        StringAssert.Contains(message.Embeds[0].Title, "maximum limit of 10 characters");
    }

    [TestMethod]
    public async Task CreateAsync_WhenNeitherPathbuilderIdNorJsonFileProvided_ReturnsError()
    {
        _userService.GetByIdAsync(42).Returns(new UserReadDto(42, "Tester", new UserSettings()));
        _characterService.GetCountByUserIdAsync(42).Returns(0);

        var message = await _sut.CreateAsync(
            callerId: 42,
            callerUsername: "Tester",
            color: "Blue",
            pathbuilderId: "   ",
            tokenUrl: "http://example.com/token.png",
            tokenFileName: "token.png",
            actorUrl: "http://example.com/actor.png",
            actorFileName: "actor.png",
            jsonFileUrl: null);

        Assert.HasCount(1, message.Embeds);
        StringAssert.Contains(message.Embeds[0].Title, "provide either a Pathbuilder JSON ID or an exported Pathbuilder JSON file");
    }

    [TestMethod]
    public async Task CreateAsync_WhenJsonFileProvided_CreatesCharacterSuccessfully()
    {
        _userService.GetByIdAsync(42).Returns(new UserReadDto(42, "Tester", new UserSettings()));
        _characterService.GetCountByUserIdAsync(42).Returns(0);
        _pathbuilderService.FetchCharacterFromUrlAsync("http://example.com/character.json", null)
            .Returns(new PathbuilderCharacterDto("Edward \"Archer\" Callahan", "Human", "Gunslinger", "5", "19", "Male", 8.6m, null));
        _characterService.GetByUserAndNameAsync(42, "Edward \"Archer\" Callahan").Returns((CharacterReadDto?)null);

        _fileService.SaveAttachmentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(callInfo => $"{callInfo.ArgAt<string>(1)}/{callInfo.ArgAt<string>(2)}");

        var message = await _sut.CreateAsync(
            callerId: 42,
            callerUsername: "Tester",
            color: "Red",
            pathbuilderId: null,
            tokenUrl: "http://example.com/token.png",
            tokenFileName: "token.png",
            actorUrl: "http://example.com/actor.png",
            actorFileName: "actor.png",
            description: "Hired killer from Ravounel.",
            jsonFileUrl: "http://example.com/character.json");

        await _characterService.Received(1).CreateAsync(Arg.Is<CharacterCreateDto>(dto =>
            dto.UserId == 42 &&
            dto.Name == "Edward \"Archer\" Callahan" &&
            dto.Color == Color.Red &&
            dto.Gold == 8.6m &&
            dto.PathbuilderId == null &&
            dto.Details.Class == "Gunslinger" &&
            dto.Details.Level == "5"));

        Assert.HasCount(1, message.Embeds);
        StringAssert.Contains(message.Embeds[0].Title, "Character 'Edward \"Archer\" Callahan' created successfully.");
    }

    [TestMethod]
    public async Task CreateAsync_WhenJsonFileParseFails_ReturnsError()
    {
        _userService.GetByIdAsync(42).Returns(new UserReadDto(42, "Tester", new UserSettings()));
        _characterService.GetCountByUserIdAsync(42).Returns(0);
        _pathbuilderService.FetchCharacterFromUrlAsync("http://example.com/bad.json", null)
            .Returns((PathbuilderCharacterDto?)null);

        var message = await _sut.CreateAsync(
            callerId: 42,
            callerUsername: "Tester",
            color: "Blue",
            pathbuilderId: null,
            tokenUrl: "http://example.com/token.png",
            tokenFileName: "token.png",
            actorUrl: "http://example.com/actor.png",
            actorFileName: "actor.png",
            jsonFileUrl: "http://example.com/bad.json");

        Assert.HasCount(1, message.Embeds);
        StringAssert.Contains(message.Embeds[0].Title, "Could not parse character data from the provided JSON file");
    }

    [TestMethod]
    public async Task CreateAsync_WhenPathbuilderFetchFails_ReturnsError()
    {
        _userService.GetByIdAsync(42).Returns(new UserReadDto(42, "Tester", new UserSettings()));
        _characterService.GetCountByUserIdAsync(42).Returns(0);
        _pathbuilderService.FetchCharacterAsync("12345").Returns((PathbuilderCharacterDto?)null);

        var message = await _sut.CreateAsync(
            callerId: 42,
            callerUsername: "Tester",
            color: "Blue",
            pathbuilderId: "12345",
            tokenUrl: "http://example.com/token.png",
            tokenFileName: "token.png",
            actorUrl: "http://example.com/actor.png",
            actorFileName: "actor.png");

        Assert.HasCount(1, message.Embeds);
        StringAssert.Contains(message.Embeds[0].Title, "Could not retrieve Pathbuilder character with ID");
    }

    [TestMethod]
    public async Task CreateAsync_WhenCharacterNameAlreadyExists_ReturnsWarning()
    {
        _userService.GetByIdAsync(42).Returns(new UserReadDto(42, "Tester", new UserSettings()));
        _characterService.GetCountByUserIdAsync(42).Returns(0);
        _pathbuilderService.FetchCharacterAsync("12345").Returns(CreatePbDto("Sunaki"));
        _characterService.GetByUserAndNameAsync(42, "Sunaki").Returns(CreateCharDto(name: "Sunaki"));

        var message = await _sut.CreateAsync(
            callerId: 42,
            callerUsername: "Tester",
            color: "Blue",
            pathbuilderId: "12345",
            tokenUrl: "http://example.com/token.png",
            tokenFileName: "token.png",
            actorUrl: "http://example.com/actor.png",
            actorFileName: "actor.png");

        Assert.HasCount(1, message.Embeds);
        StringAssert.Contains(message.Embeds[0].Title, "You already have an active character named");
    }

    [TestMethod]
    public async Task CreateAsync_WhenValid_SavesImagesAndCreatesCharacter()
    {
        _userService.GetByIdAsync(42).Returns(new UserReadDto(42, "Tester", new UserSettings()));
        _characterService.GetCountByUserIdAsync(42).Returns(2);
        _pathbuilderService.SanitizeExportId("12345").Returns("12345");
        _pathbuilderService.FetchCharacterAsync("12345").Returns(CreatePbDto("Sunaki"));
        _characterService.GetByUserAndNameAsync(42, "Sunaki").Returns((CharacterReadDto?)null);

        _fileService.SaveAttachmentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(callInfo => $"{callInfo.ArgAt<string>(1)}/{callInfo.ArgAt<string>(2)}");

        var message = await _sut.CreateAsync(
            callerId: 42,
            callerUsername: "Tester",
            color: "Blue",
            pathbuilderId: "12345",
            tokenUrl: "http://example.com/token.png",
            tokenFileName: "token.png",
            actorUrl: "http://example.com/actor.png",
            actorFileName: "actor.png",
            description: "A stealthy rogue.");

        await _characterService.Received(1).CreateAsync(Arg.Is<CharacterCreateDto>(dto =>
            dto.UserId == 42 &&
            dto.Name == "Sunaki" &&
            dto.Color == Color.Blue &&
            dto.Gold == 10.5m &&
            dto.PathbuilderId == "12345" &&
            dto.Details.Class == "Rogue" &&
            dto.Description == "A stealthy rogue."));

        Assert.HasCount(1, message.Embeds);
        StringAssert.Contains(message.Embeds[0].Title, "Character 'Sunaki' created successfully.");
    }

    [TestMethod]
    public async Task CreateAsync_WhenImageSaveThrows_ReturnsError()
    {
        _userService.GetByIdAsync(42).Returns(new UserReadDto(42, "Tester", new UserSettings()));
        _characterService.GetCountByUserIdAsync(42).Returns(0);
        _pathbuilderService.SanitizeExportId("12345").Returns("12345");
        _pathbuilderService.FetchCharacterAsync("12345").Returns(CreatePbDto("Sunaki"));
        _characterService.GetByUserAndNameAsync(42, "Sunaki").Returns((CharacterReadDto?)null);

        _fileService.SaveAttachmentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .ThrowsAsync(new IOException("Disk error"));

        var message = await _sut.CreateAsync(
            callerId: 42,
            callerUsername: "Tester",
            color: "Blue",
            pathbuilderId: "12345",
            tokenUrl: "http://example.com/token.png",
            tokenFileName: "token.png",
            actorUrl: "http://example.com/actor.png",
            actorFileName: "actor.png");

        Assert.HasCount(1, message.Embeds);
        StringAssert.Contains(message.Embeds[0].Title, "An error occurred while saving the character and images.");
    }

    // ---- DetailsAsync ---------------------------------------------------

    [TestMethod]
    public async Task DetailsAsync_WhenNameEmpty_ReturnsError()
    {
        var message = await _sut.DetailsAsync(42, "   ", "Tester");

        Assert.HasCount(1, message.Embeds);
        StringAssert.Contains(message.Embeds[0].Title, "Character name cannot be empty.");
    }

    [TestMethod]
    public async Task DetailsAsync_WhenCharacterNotFound_ReturnsError()
    {
        _characterService.GetByUserAndNameAsync(42, "NonExistent").Returns((CharacterReadDto?)null);

        var message = await _sut.DetailsAsync(42, "NonExistent", "Tester");

        Assert.HasCount(1, message.Embeds);
        StringAssert.Contains(message.Embeds[0].Title, "was not found.");
    }

    [TestMethod]
    public async Task DetailsAsync_WhenCharacterExists_ReturnsDetailsWithImageFiles()
    {
        var character = CreateCharDto();
        _characterService.GetByUserAndNameAsync(42, "Sunaki").Returns(character);

        var tokenStream = new MemoryStream(Encoding.UTF8.GetBytes("fake token"));
        var actorStream = new MemoryStream(Encoding.UTF8.GetBytes("fake actor"));

        _fileService.OpenRead(character.TokenImagePath).Returns(tokenStream);
        _fileService.OpenRead(character.ActorImagePath).Returns(actorStream);

        var message = await _sut.DetailsAsync(42, "Sunaki", "Tester");

        Assert.HasCount(1, message.Embeds);
        Assert.AreEqual("Human Rogue, Level 3", message.Embeds[0].Author?.Name);
        Assert.AreEqual("Sunaki", message.Embeds[0].Title);
        Assert.HasCount(2, message.Files);
        Assert.IsTrue(message.Files.Any(f => f.FileName == "token.png"));
        Assert.IsTrue(message.Files.Any(f => f.FileName == "actor.png"));
    }

    // ---- DeletePromptAsync ----------------------------------------------

    [TestMethod]
    public async Task DeletePromptAsync_WhenNameEmpty_ReturnsError()
    {
        var message = await _sut.DeletePromptAsync(42, " ");

        Assert.HasCount(1, message.Embeds);
        StringAssert.Contains(message.Embeds[0].Title, "Character name cannot be empty.");
    }

    [TestMethod]
    public async Task DeletePromptAsync_WhenNotFound_ReturnsError()
    {
        _characterService.GetByUserAndNameAsync(42, "NonExistent").Returns((CharacterReadDto?)null);

        var message = await _sut.DeletePromptAsync(42, "NonExistent");

        Assert.HasCount(1, message.Embeds);
        StringAssert.Contains(message.Embeds[0].Title, "was not found.");
    }

    [TestMethod]
    public async Task DeletePromptAsync_WhenInGame_ReturnsError()
    {
        _characterService.GetByUserAndNameAsync(42, "Sunaki")
            .Returns(CreateCharDto(currentGame: "Campaign 1"));

        var message = await _sut.DeletePromptAsync(42, "Sunaki");

        Assert.HasCount(1, message.Embeds);
        StringAssert.Contains(message.Embeds[0].Title, "cannot be deleted while participating in a game");
    }

    [TestMethod]
    public async Task DeletePromptAsync_WhenValid_ReturnsConfirmationPromptWithButtons()
    {
        _characterService.GetByUserAndNameAsync(42, "Sunaki")
            .Returns(CreateCharDto(currentGame: null));

        var message = await _sut.DeletePromptAsync(42, "Sunaki");

        Assert.HasCount(1, message.Embeds);
        Assert.AreEqual("Delete Character: Sunaki", message.Embeds[0].Title);
        Assert.HasCount(1, message.Components);
        var buttons = message.Components[0].Components;
        Assert.HasCount(2, buttons);
    }

    // ---- ConfirmDeleteAsync ---------------------------------------------

    [TestMethod]
    public async Task ConfirmDeleteAsync_WhenCallerNotOwner_ReturnsUnauthorized()
    {
        var message = await _sut.ConfirmDeleteAsync(callerId: 99, characterId: 1, ownerUserId: 42);

        Assert.HasCount(1, message.Embeds);
        StringAssert.Contains(message.Embeds[0].Title, "not authorized to delete this character");
    }

    [TestMethod]
    public async Task ConfirmDeleteAsync_WhenCharacterNotFound_ReturnsError()
    {
        _characterService.GetByIdAsync(1).Returns((CharacterReadDto?)null);

        var message = await _sut.ConfirmDeleteAsync(callerId: 42, characterId: 1, ownerUserId: 42);

        Assert.HasCount(1, message.Embeds);
        StringAssert.Contains(message.Embeds[0].Title, "not found or already deleted");
    }

    [TestMethod]
    public async Task ConfirmDeleteAsync_WhenCharacterInGame_ReturnsError()
    {
        _characterService.GetByIdAsync(1).Returns(CreateCharDto(currentGame: "Campaign 1"));

        var message = await _sut.ConfirmDeleteAsync(callerId: 42, characterId: 1, ownerUserId: 42);

        Assert.HasCount(1, message.Embeds);
        StringAssert.Contains(message.Embeds[0].Title, "cannot be deleted while participating in a game");
    }

    [TestMethod]
    public async Task ConfirmDeleteAsync_WhenValid_DeletesCharacterAndImageDirectory()
    {
        var character = CreateCharDto(id: 1, userId: 42, name: "Sunaki");
        _characterService.GetByIdAsync(1).Returns(character);

        var message = await _sut.ConfirmDeleteAsync(callerId: 42, characterId: 1, ownerUserId: 42);

        await _characterService.Received(1).DeleteAsync(1);
        _fileService.Received(1).DeleteDirectory("characters/42/Sunaki");

        Assert.HasCount(1, message.Embeds);
        StringAssert.Contains(message.Embeds[0].Title, "Character 'Sunaki' was permanently deleted.");
    }

    [TestMethod]
    public async Task ConfirmDeleteAsync_WhenDeleteThrows_ReturnsError()
    {
        _characterService.GetByIdAsync(1).Returns(CreateCharDto(id: 1, userId: 42));
        _characterService.DeleteAsync(1).ThrowsAsync(new InvalidOperationException("DB error"));

        var message = await _sut.ConfirmDeleteAsync(callerId: 42, characterId: 1, ownerUserId: 42);

        Assert.HasCount(1, message.Embeds);
        StringAssert.Contains(message.Embeds[0].Title, "An error occurred while deleting the character.");
    }

    // ---- EditAsync ------------------------------------------------------

    [TestMethod]
    public async Task EditAsync_WhenNameEmpty_ReturnsError()
    {
        var message = await _sut.EditAsync(42, " ");

        Assert.HasCount(1, message.Embeds);
        StringAssert.Contains(message.Embeds[0].Title, "Character name cannot be empty.");
    }

    [TestMethod]
    public async Task EditAsync_WhenNotFound_ReturnsError()
    {
        _characterService.GetByUserAndNameAsync(42, "NonExistent").Returns((CharacterReadDto?)null);

        var message = await _sut.EditAsync(42, "NonExistent");

        Assert.HasCount(1, message.Embeds);
        StringAssert.Contains(message.Embeds[0].Title, "was not found.");
    }

    [TestMethod]
    public async Task EditAsync_WhenInGame_ReturnsError()
    {
        _characterService.GetByUserAndNameAsync(42, "Sunaki")
            .Returns(CreateCharDto(currentGame: "Adventure 1"));

        var message = await _sut.EditAsync(42, "Sunaki");

        Assert.HasCount(1, message.Embeds);
        StringAssert.Contains(message.Embeds[0].Title, "cannot be edited while participating in a game");
    }

    [TestMethod]
    public async Task EditAsync_WhenNoUpdatesProvided_ReturnsWarning()
    {
        _characterService.GetByUserAndNameAsync(42, "Sunaki")
            .Returns(CreateCharDto(currentGame: null));

        var message = await _sut.EditAsync(42, "Sunaki");

        Assert.HasCount(1, message.Embeds);
        StringAssert.Contains(message.Embeds[0].Title, "No fields were provided to update");
    }

    [TestMethod]
    public async Task EditAsync_WhenColorAndDescriptionProvided_UpdatesCharacter()
    {
        var character = CreateCharDto(id: 1, userId: 42, name: "Sunaki");
        _characterService.GetByUserAndNameAsync(42, "Sunaki").Returns(character);

        var message = await _sut.EditAsync(
            callerId: 42,
            characterName: "Sunaki",
            color: "Purple",
            description: "Updated backstory");

        await _characterService.Received(1).UpdateAsync(Arg.Is<CharacterUpdateDto>(dto =>
            dto.Id == 1 &&
            dto.Color == Color.Purple &&
            dto.Description == "Updated backstory" &&
            dto.TokenImagePath == character.TokenImagePath &&
            dto.ActorImagePath == character.ActorImagePath));

        Assert.HasCount(1, message.Embeds);
        StringAssert.Contains(message.Embeds[0].Title, "Character 'Sunaki' updated successfully.");
    }

    [TestMethod]
    public async Task EditAsync_WhenTokenImageProvided_ReplacesOldToken()
    {
        var character = CreateCharDto(id: 1, userId: 42, name: "Sunaki");
        _characterService.GetByUserAndNameAsync(42, "Sunaki").Returns(character);

        _fileService.SaveAttachmentAsync("http://example.com/newToken.png", Arg.Any<string>(), Arg.Any<string>())
            .Returns("characters/42/Sunaki/token_new.png");

        var message = await _sut.EditAsync(
            callerId: 42,
            characterName: "Sunaki",
            tokenUrl: "http://example.com/newToken.png",
            tokenFileName: "newToken.png");

        _fileService.Received(1).DeleteFile(character.TokenImagePath);
        await _characterService.Received(1).UpdateAsync(Arg.Is<CharacterUpdateDto>(dto =>
            dto.Id == 1 &&
            dto.TokenImagePath == "characters/42/Sunaki/token_new.png"));

        Assert.HasCount(1, message.Embeds);
        StringAssert.Contains(message.Embeds[0].Title, "Character 'Sunaki' updated successfully.");
    }

    [TestMethod]
    public async Task EditAsync_WhenPathbuilderProvided_UpdatesDetailsAndGold()
    {
        var character = CreateCharDto(id: 1, userId: 42, name: "Sunaki");
        _characterService.GetByUserAndNameAsync(42, "Sunaki").Returns(character);

        _pathbuilderService.SanitizeExportId("67890").Returns("67890");
        _pathbuilderService.FetchCharacterAsync("67890")
            .Returns(new PathbuilderCharacterDto("Sunaki", "Elf", "Wizard", "5", "120", "Female", 50.0m, "67890"));

        var message = await _sut.EditAsync(
            callerId: 42,
            characterName: "Sunaki",
            pathbuilderId: "67890");

        await _characterService.Received(1).UpdateAsync(Arg.Is<CharacterUpdateDto>(dto =>
            dto.Id == 1 &&
            dto.Gold == 50.0m &&
            dto.PathbuilderId == "67890" &&
            dto.Details != null &&
            dto.Details.Ancestry == "Elf" &&
            dto.Details.Class == "Wizard" &&
            dto.Details.Level == "5"));

        Assert.HasCount(1, message.Embeds);
        StringAssert.Contains(message.Embeds[0].Title, "Character 'Sunaki' updated successfully.");
    }

    [TestMethod]
    public async Task EditAsync_WhenJsonFileProvided_UpdatesDetailsAndGold()
    {
        var character = CreateCharDto(id: 1, userId: 42, name: "Sunaki");
        _characterService.GetByUserAndNameAsync(42, "Sunaki").Returns(character);

        _pathbuilderService.FetchCharacterFromUrlAsync("http://example.com/character.json", "12345")
            .Returns(new PathbuilderCharacterDto("Sunaki", "Human", "Gunslinger", "5", "19", "Male", 8.6m, "12345"));

        var message = await _sut.EditAsync(
            callerId: 42,
            characterName: "Sunaki",
            jsonFileUrl: "http://example.com/character.json");

        await _characterService.Received(1).UpdateAsync(Arg.Is<CharacterUpdateDto>(dto =>
            dto.Id == 1 &&
            dto.Gold == 8.6m &&
            dto.PathbuilderId == "12345" &&
            dto.Details != null &&
            dto.Details.Class == "Gunslinger" &&
            dto.Details.Level == "5"));

        Assert.HasCount(1, message.Embeds);
        StringAssert.Contains(message.Embeds[0].Title, "Character 'Sunaki' updated successfully.");
    }

    // ---- CanInteractWithCharacterDeletion -------------------------------

    [TestMethod]
    public void CanInteractWithCharacterDeletion_WhenCallerIsOwner_ReturnsTrue()
    {
        Assert.IsTrue(Voyager.API.Events.EventHandler.CanInteractWithCharacterDeletion(42, 42));
    }

    [TestMethod]
    public void CanInteractWithCharacterDeletion_WhenCallerIsNotOwner_ReturnsFalse()
    {
        Assert.IsFalse(Voyager.API.Events.EventHandler.CanInteractWithCharacterDeletion(99, 42));
    }
}
