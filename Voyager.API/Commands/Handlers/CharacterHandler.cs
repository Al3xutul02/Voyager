using BusinessLogic.Dtos.Character;
using BusinessLogic.Dtos.Pathbuilder;
using BusinessLogic.Enums.Types;
using BusinessLogic.Json.Models;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;
using Voyager.API.Enums.Types;
using Voyager.API.Views;

namespace Voyager.API.Commands.Handlers;

/// <summary>
/// Testable core of the <c>/character</c> command group (<c>create</c>, <c>details</c>,
/// <c>delete</c>, <c>edit</c>) and confirmation interactions.
/// </summary>
public class CharacterHandler(
    ICharacterService characterService,
    IUserService userService,
    IPathbuilderService pathbuilderService,
    IFileService fileService,
    IEnumService enumService,
    ViewFactory viewFactory)
{
    private readonly ICharacterService _characterService = characterService;
    private readonly IUserService _userService = userService;
    private readonly IPathbuilderService _pathbuilderService = pathbuilderService;
    private readonly IFileService _fileService = fileService;
    private readonly IEnumService _enumService = enumService;
    private readonly ViewFactory _viewFactory = viewFactory;

    /// <summary>
    /// Builds the response for <c>/character create</c>.
    /// Verifies the caller has a user profile, enforces the 10-character limit,
    /// imports data from Pathbuilder 2e (either via export ID or uploaded JSON file),
    /// downloads images to local disk, and persists the new character.
    /// </summary>
    public async Task<DiscordMessageBuilder> CreateAsync(
        ulong callerId,
        string callerUsername,
        string color,
        string? pathbuilderId,
        string tokenUrl,
        string tokenFileName,
        string actorUrl,
        string actorFileName,
        string? description = null,
        string? jsonFileUrl = null)
    {
        var user = await _userService.GetByIdAsync(callerId);
        if (user is null)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: You need to create a profile first before creating a character. Use /profile create.");
        }

        var currentCount = await _characterService.GetCountByUserIdAsync(callerId);
        if (currentCount >= ICharacterService.MaxCharactersPerUser)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                $"Error: You have reached the maximum limit of {ICharacterService.MaxCharactersPerUser} characters.");
        }

        bool hasPathbuilderId = !string.IsNullOrWhiteSpace(pathbuilderId);
        bool hasJsonFile = !string.IsNullOrWhiteSpace(jsonFileUrl);

        if (!hasPathbuilderId && !hasJsonFile)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: You must provide either a Pathbuilder JSON ID or an exported Pathbuilder JSON file.");
        }

        PathbuilderCharacterDto? pbCharacter;
        if (hasJsonFile)
        {
            var optionalPbId = hasPathbuilderId
                ? _pathbuilderService.SanitizeExportId(pathbuilderId!)
                : null;
            pbCharacter = await _pathbuilderService.FetchCharacterFromUrlAsync(jsonFileUrl!, optionalPbId);
            if (pbCharacter is null)
            {
                return _viewFactory.CreateNotification(
                    NotificationType.Error,
                    "Error: Could not parse character data from the provided JSON file. Please verify it is a valid Pathbuilder 2e JSON export.");
            }
        }
        else
        {
            pbCharacter = await _pathbuilderService.FetchCharacterAsync(pathbuilderId!);
            if (pbCharacter is null)
            {
                return _viewFactory.CreateNotification(
                    NotificationType.Error,
                    $"Error: Could not retrieve Pathbuilder character with ID '{pathbuilderId}'. Please verify the ID or upload the exported JSON file directly.");
            }
        }

        var existing = await _characterService.GetByUserAndNameAsync(callerId, pbCharacter.Name);
        if (existing is not null)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Warning,
                $"Warning: You already have an active character named '{pbCharacter.Name}'.");
        }

        var paletteColor = !string.IsNullOrWhiteSpace(color)
            ? _enumService.ConvertColor(color)
            : user.Settings.Color;

        try
        {
            var folderName = Guid.NewGuid().ToString("N");
            var relativeDir = $"Characters/{callerId}/{folderName}";

            var tokenExt = Path.GetExtension(tokenFileName);
            if (string.IsNullOrWhiteSpace(tokenExt)) tokenExt = ".png";
            var actorExt = Path.GetExtension(actorFileName);
            if (string.IsNullOrWhiteSpace(actorExt)) actorExt = ".png";

            var tokenPath = await _fileService.SaveAttachmentAsync(tokenUrl, relativeDir, $"token{tokenExt}");
            var actorPath = await _fileService.SaveAttachmentAsync(actorUrl, relativeDir, $"actor{actorExt}");

            var createDto = new CharacterCreateDto(
                UserId: callerId,
                Name: pbCharacter.Name,
                Description: description,
                Color: paletteColor,
                TokenImagePath: tokenPath,
                ActorImagePath: actorPath,
                CurrentGame: null,
                Gold: pbCharacter.Gold,
                PathbuilderId: pbCharacter.PathbuilderId,
                Details: new CharacterDetails(
                    Ancestry: pbCharacter.Ancestry,
                    Class: pbCharacter.Class,
                    Level: pbCharacter.Level,
                    Age: pbCharacter.Age,
                    Gender: pbCharacter.Gender
                )
            );

            await _characterService.CreateAsync(createDto);

            return _viewFactory.CreateNotification(
                NotificationType.Success,
                $"Character '{pbCharacter.Name}' created successfully.");
        }
        catch (Exception)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "An error occurred while saving the character and images.");
        }
    }

    /// <summary>
    /// Builds the response for <c>/character details</c>.
    /// Renders the character sheet embed with attached token and actor portrait images.
    /// </summary>
    public async Task<DiscordMessageBuilder> DetailsAsync(
        ulong callerId,
        string characterName,
        string playerName)
    {
        if (string.IsNullOrWhiteSpace(characterName))
        {
            return _viewFactory.CreateNotification(NotificationType.Error, "Error: Character name cannot be empty.");
        }

        var character = await _characterService.GetByUserAndNameAsync(callerId, characterName.Trim());
        if (character is null)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                $"Error: Character '{characterName.Trim()}' was not found.");
        }

        Stream? tokenStream = _fileService.OpenRead(character.TokenImagePath);
        Stream? actorStream = _fileService.OpenRead(character.ActorImagePath);

        return _viewFactory.CreateCharacterDetails(character, playerName, tokenStream, actorStream);
    }

    /// <summary>
    /// Builds the response for <c>/character delete</c>.
    /// Displays an irreversible warning embed with Confirm Delete and Cancel buttons.
    /// </summary>
    public async Task<DiscordMessageBuilder> DeletePromptAsync(
        ulong callerId,
        string characterName)
    {
        if (string.IsNullOrWhiteSpace(characterName))
        {
            return _viewFactory.CreateNotification(NotificationType.Error, "Error: Character name cannot be empty.");
        }

        var character = await _characterService.GetByUserAndNameAsync(callerId, characterName.Trim());
        if (character is null)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                $"Error: Character '{characterName.Trim()}' was not found.");
        }

        if (!string.IsNullOrWhiteSpace(character.CurrentGame))
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                $"Error: Character '{character.Name}' cannot be deleted while participating in a game ('{character.CurrentGame}').");
        }

        return _viewFactory.CreateCharacterDeleteConfirmation(character, callerId);
    }

    /// <summary>
    /// Executes character deletion after the user clicks the "Confirm Delete" button.
    /// </summary>
    public async Task<DiscordMessageBuilder> ConfirmDeleteAsync(
        ulong callerId,
        ulong characterId,
        ulong ownerUserId)
    {
        if (callerId != ownerUserId)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: You are not authorized to delete this character.");
        }

        var character = await _characterService.GetByIdAsync(characterId);
        if (character is null || character.UserId != ownerUserId)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: Character not found or already deleted.");
        }

        if (!string.IsNullOrWhiteSpace(character.CurrentGame))
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                $"Error: Character '{character.Name}' cannot be deleted while participating in a game ('{character.CurrentGame}').");
        }

        try
        {
            await _characterService.DeleteAsync(characterId);

            if (!string.IsNullOrWhiteSpace(character.TokenImagePath))
            {
                var dir = Path.GetDirectoryName(character.TokenImagePath)?.Replace('\\', '/');
                if (!string.IsNullOrWhiteSpace(dir))
                {
                    _fileService.DeleteDirectory(dir);
                }
            }

            return _viewFactory.CreateNotification(
                NotificationType.Success,
                $"Character '{character.Name}' was permanently deleted.");
        }
        catch (Exception)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "An error occurred while deleting the character.");
        }
    }

    /// <summary>
    /// Builds the response for <c>/character edit</c>.
    /// Updates optional image, color, Pathbuilder build (via ID or JSON file), or description fields.
    /// Rejects if the character is currently participating in a game.
    /// </summary>
    public async Task<DiscordMessageBuilder> EditAsync(
        ulong callerId,
        string characterName,
        string? tokenUrl = null,
        string? tokenFileName = null,
        string? actorUrl = null,
        string? actorFileName = null,
        string? color = null,
        string? pathbuilderId = null,
        string? description = null,
        string? jsonFileUrl = null)
    {
        if (string.IsNullOrWhiteSpace(characterName))
        {
            return _viewFactory.CreateNotification(NotificationType.Error, "Error: Character name cannot be empty.");
        }

        var character = await _characterService.GetByUserAndNameAsync(callerId, characterName.Trim());
        if (character is null)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                $"Error: Character '{characterName.Trim()}' was not found.");
        }

        if (!string.IsNullOrWhiteSpace(character.CurrentGame))
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                $"Error: Character '{character.Name}' cannot be edited while participating in a game ('{character.CurrentGame}').");
        }

        if (string.IsNullOrWhiteSpace(tokenUrl) &&
            string.IsNullOrWhiteSpace(actorUrl) &&
            string.IsNullOrWhiteSpace(color) &&
            string.IsNullOrWhiteSpace(pathbuilderId) &&
            string.IsNullOrWhiteSpace(jsonFileUrl) &&
            description is null)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Warning,
                "Warning: No fields were provided to update.");
        }

        try
        {
            var tokenPath = character.TokenImagePath;
            var actorPath = character.ActorImagePath;
            var dir = Path.GetDirectoryName(character.TokenImagePath)?.Replace('\\', '/')
                ?? $"Characters/{callerId}/{Guid.NewGuid():N}";

            if (!string.IsNullOrWhiteSpace(tokenUrl) && !string.IsNullOrWhiteSpace(tokenFileName))
            {
                var ext = Path.GetExtension(tokenFileName);
                if (string.IsNullOrWhiteSpace(ext)) ext = ".png";
                tokenPath = await _fileService.SaveAttachmentAsync(tokenUrl, dir, $"token{ext}");
                if (tokenPath != character.TokenImagePath && !string.IsNullOrWhiteSpace(character.TokenImagePath))
                {
                    _fileService.DeleteFile(character.TokenImagePath);
                }
            }

            if (!string.IsNullOrWhiteSpace(actorUrl) && !string.IsNullOrWhiteSpace(actorFileName))
            {
                var ext = Path.GetExtension(actorFileName);
                if (string.IsNullOrWhiteSpace(ext)) ext = ".png";
                actorPath = await _fileService.SaveAttachmentAsync(actorUrl, dir, $"actor{ext}");
                if (actorPath != character.ActorImagePath && !string.IsNullOrWhiteSpace(character.ActorImagePath))
                {
                    _fileService.DeleteFile(character.ActorImagePath);
                }
            }

            var updatedName = character.Name;
            var updatedGold = character.Gold;
            var updatedPbId = character.PathbuilderId;
            var updatedDetails = character.Details;

            if (!string.IsNullOrWhiteSpace(jsonFileUrl))
            {
                var optionalPbId = !string.IsNullOrWhiteSpace(pathbuilderId)
                    ? _pathbuilderService.SanitizeExportId(pathbuilderId)
                    : character.PathbuilderId;
                var pbChar = await _pathbuilderService.FetchCharacterFromUrlAsync(jsonFileUrl, optionalPbId);
                if (pbChar is null)
                {
                    return _viewFactory.CreateNotification(
                        NotificationType.Error,
                        "Error: Could not parse character data from the provided JSON file. Please verify it is a valid Pathbuilder 2e JSON export.");
                }

                updatedName = pbChar.Name;
                updatedGold = pbChar.Gold;
                updatedPbId = pbChar.PathbuilderId;
                updatedDetails = new CharacterDetails(
                    Ancestry: pbChar.Ancestry,
                    Class: pbChar.Class,
                    Level: pbChar.Level,
                    Age: pbChar.Age,
                    Gender: pbChar.Gender
                );
            }
            else if (!string.IsNullOrWhiteSpace(pathbuilderId))
            {
                var pbChar = await _pathbuilderService.FetchCharacterAsync(pathbuilderId);
                if (pbChar is null)
                {
                    return _viewFactory.CreateNotification(
                        NotificationType.Error,
                        $"Error: Could not retrieve Pathbuilder character with ID '{pathbuilderId}'. Please verify the ID or upload the exported JSON file directly.");
                }

                updatedName = pbChar.Name;
                updatedGold = pbChar.Gold;
                updatedPbId = pbChar.PathbuilderId;
                updatedDetails = new CharacterDetails(
                    Ancestry: pbChar.Ancestry,
                    Class: pbChar.Class,
                    Level: pbChar.Level,
                    Age: pbChar.Age,
                    Gender: pbChar.Gender
                );
            }

            var updatedColor = !string.IsNullOrWhiteSpace(color)
                ? _enumService.ConvertColor(color)
                : character.Color;

            var updatedDescription = description ?? character.Description;

            var updateDto = new CharacterUpdateDto(
                Id: character.Id,
                UserId: callerId,
                Name: updatedName,
                Description: updatedDescription,
                Color: updatedColor,
                TokenImagePath: tokenPath,
                ActorImagePath: actorPath,
                CurrentGame: character.CurrentGame,
                Gold: updatedGold,
                PathbuilderId: updatedPbId,
                Details: updatedDetails
            );

            await _characterService.UpdateAsync(updateDto);

            return _viewFactory.CreateNotification(
                NotificationType.Success,
                $"Character '{updatedName}' updated successfully.");
        }
        catch (Exception)
        {
            return _viewFactory.CreateNotification(
                NotificationType.Error,
                "An error occurred while updating the character.");
        }
    }
}
