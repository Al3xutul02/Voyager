using BusinessLogic.Dtos.Server;
using BusinessLogic.Services.Generic;
using Repository.Models;

namespace BusinessLogic.Services.Abstractions;

public interface IServerService
    : IBaseService<ulong, Server, ServerReadDto, ServerCreateDto, ServerUpdateDto>
{
    public Task<ServerReadDto?> GetByName(string name);
}
