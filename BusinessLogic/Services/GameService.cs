using AutoMapper;
using BusinessLogic.Dtos.Game;
using BusinessLogic.Services.Abstractions;
using BusinessLogic.Services.Generic;
using Microsoft.EntityFrameworkCore;
using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories.Abstractions;

namespace BusinessLogic.Services;

/// <summary>
/// Service implementation for managing games and player enrollments.
/// </summary>
public class GameService(
    IMapper mapper,
    IGameRepository gameRepository,
    ICharacterRepository characterRepository)
    : BaseService<ulong, Game, GameReadDto, GameCreateDto, GameUpdateDto>(mapper, gameRepository), IGameService
{
    private readonly ICharacterRepository _characterRepository = characterRepository;
    private IGameRepository GameRepository => (IGameRepository)_repository;

    private static IQueryable<Game> IncludeGameRelations(IQueryable<Game> query) =>
        query.Include(g => g.GameMaster)
             .Include(g => g.Players)
                 .ThenInclude(p => p.User);

    /// <inheritdoc />
    public override async Task<GameReadDto?> GetByIdAsync(ulong id)
    {
        var game = await _repository.GetByIdAsync(id, IncludeBehavior.GivenIncludes, IncludeGameRelations);
        if (game is null || game.DeletedAt != null)
        {
            return null;
        }

        return _mapper.Map<GameReadDto>(game);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<GameReadDto>> GetByServerIdAsync(ulong serverId)
    {
        var games = await GameRepository.GetByServerIdAsync(serverId, IncludeBehavior.GivenIncludes, IncludeGameRelations);
        return _mapper.Map<IEnumerable<GameReadDto>>(games.Where(g => g.DeletedAt == null));
    }

    /// <inheritdoc />
    public async Task<GameReadDto?> GetByServerAndNameAsync(ulong serverId, string name)
    {
        var game = await GameRepository.GetByServerAndNameAsync(serverId, name, IncludeBehavior.GivenIncludes, IncludeGameRelations);
        if (game is null || game.DeletedAt != null)
        {
            return null;
        }

        return _mapper.Map<GameReadDto>(game);
    }

    /// <inheritdoc />
    public async Task<GameReadDto?> GetByChannelIdAsync(ulong serverId, ulong channelId)
    {
        var game = await GameRepository.GetByChannelIdAsync(serverId, channelId, IncludeBehavior.GivenIncludes, IncludeGameRelations);
        if (game is null || game.DeletedAt != null)
        {
            return null;
        }

        return _mapper.Map<GameReadDto>(game);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<GameReadDto>> GetByUserIdAsync(ulong userId)
    {
        var games = await GameRepository.GetByUserIdAsync(userId, IncludeBehavior.GivenIncludes, IncludeGameRelations);
        return _mapper.Map<IEnumerable<GameReadDto>>(games.Where(g => g.DeletedAt == null));
    }

    /// <inheritdoc />
    public async Task<int> GetCountByServerIdAsync(ulong serverId)
    {
        return await GameRepository.GetCountByServerIdAsync(serverId);
    }

    /// <inheritdoc />
    public async Task<bool> IsPlayerInGameAsync(ulong gameId, ulong userId)
    {
        return await GameRepository.IsPlayerInGameAsync(gameId, userId);
    }

    /// <inheritdoc />
    public async Task AddPlayerAsync(ulong gameId, ulong userId)
    {
        if (await GameRepository.IsPlayerInGameAsync(gameId, userId))
        {
            return;
        }

        await GameRepository.AddPlayerAsync(new GamePlayer
        {
            GameId = gameId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        });
        await _repository.SaveAsync();
    }

    /// <inheritdoc />
    public override async Task UpdateAsync(GameUpdateDto updateModel)
    {
        var existing = await _repository.GetByIdAsync(updateModel.Id, IncludeBehavior.NoInclude)
            ?? throw new KeyNotFoundException($"Game with id {updateModel.Id} not found.");

        var oldName = existing.Name;
        var entity = _mapper.Map<Game>(updateModel);
        entity.CreatedAt = existing.CreatedAt;
        entity.UpdatedAt = DateTime.UtcNow;

        _repository.Update(entity);

        if (!string.Equals(oldName, updateModel.Name, StringComparison.Ordinal))
        {
            await _characterRepository.UpdateCurrentGameAsync(oldName, updateModel.Name);
        }

        await _repository.SaveAsync();
    }

    /// <inheritdoc />
    public override async Task DeleteAsync(ulong id)
    {
        var entity = await _repository.GetByIdAsync(id, IncludeBehavior.NoInclude)
            ?? throw new KeyNotFoundException($"Game with id {id} not found.");

        entity.DeletedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        _repository.Update(entity);

        await _characterRepository.UpdateCurrentGameAsync(entity.Name, null);
        await _repository.SaveAsync();
    }
}
