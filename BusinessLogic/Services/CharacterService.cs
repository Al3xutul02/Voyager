using AutoMapper;
using BusinessLogic.Dtos.Character;
using BusinessLogic.Services.Abstractions;
using BusinessLogic.Services.Generic;
using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories.Abstractions;

namespace BusinessLogic.Services;

/// <summary>
/// Service implementation for managing user characters.
/// </summary>
public class CharacterService(IMapper mapper, ICharacterRepository characterRepository)
    : BaseService<ulong, Character, CharacterReadDto, CharacterCreateDto, CharacterUpdateDto>(mapper, characterRepository), ICharacterService
{
    private ICharacterRepository CharacterRepository => (ICharacterRepository)_repository;

    /// <inheritdoc />
    public async Task<IEnumerable<CharacterReadDto>> GetByUserIdAsync(ulong userId)
    {
        var characters = await CharacterRepository.GetByUserIdAsync(userId, IncludeBehavior.NoInclude);
        return _mapper.Map<IEnumerable<CharacterReadDto>>(characters.Where(c => c.DeletedAt == null));
    }

    /// <inheritdoc />
    public async Task<CharacterReadDto?> GetByUserAndNameAsync(ulong userId, string name)
    {
        var character = await CharacterRepository.GetByUserAndNameAsync(userId, name, IncludeBehavior.NoInclude);
        if (character is null || character.DeletedAt != null)
        {
            return null;
        }

        return _mapper.Map<CharacterReadDto>(character);
    }

    /// <inheritdoc />
    public async Task<int> GetCountByUserIdAsync(ulong userId)
    {
        return await CharacterRepository.GetCountByUserIdAsync(userId);
    }

    /// <inheritdoc />
    public override async Task DeleteAsync(ulong id)
    {
        var entity = await _repository.GetByIdAsync(id, IncludeBehavior.NoInclude)
            ?? throw new KeyNotFoundException($"Character with id {id} not found.");

        entity.DeletedAt = DateTime.UtcNow;
        _repository.Update(entity);
        await _repository.SaveAsync();
    }
}
