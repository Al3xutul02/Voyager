using AutoMapper;
using BusinessLogic.Dtos.Server;
using BusinessLogic.Services.Abstractions;
using BusinessLogic.Services.Generic;
using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories.Abstractions;

namespace BusinessLogic.Services;

/// <summary>
/// Business logic service for handling Discord server configuration and information.
/// </summary>
/// <param name="mapper">The AutoMapper instance used for entity-DTO mapping.</param>
/// <param name="serverRepository">The repository used to query and persist server entities.</param>
public class ServerService(IMapper mapper, IServerRepository serverRepository)
    : BaseService<ulong, Server, ServerReadDto, ServerCreateDto, ServerUpdateDto>(mapper, serverRepository), IServerService
{
    private IServerRepository ServerRepository => (IServerRepository)_repository;

    /// <inheritdoc />
    public async Task<ServerReadDto?> GetByName(string name)
    {
        Server? server = await ServerRepository.GetByNameAsync(name, IncludeBehavior.NoInclude);
        return _mapper.Map<ServerReadDto>(server);
    }
}
