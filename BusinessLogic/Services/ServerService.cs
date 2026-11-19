using AutoMapper;
using BusinessLogic.Dtos.Server;
using BusinessLogic.Services.Abstractions;
using BusinessLogic.Services.Generic;
using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories.Abstractions;

namespace BusinessLogic.Services;

public class ServerService(IMapper mapper, IServerRepository serverRepository)
    : BaseService<ulong, Server, ServerReadDto, ServerCreateDto, ServerUpdateDto>(mapper, serverRepository), IServerService
{
    private IServerRepository ServerRepository => (IServerRepository)_repository;

    public async Task<ServerReadDto?> GetByName(string name)
    {
        Server? server = await ServerRepository.GetByNameAsync(name, IncludeBehavior.NoInclude);
        return _mapper.Map<ServerReadDto>(server);
    }
}
