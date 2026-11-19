using BusinessLogic.Dtos.Server;
using BusinessLogic.Services.Generic;
using Repository.Models;

namespace BusinessLogic.Services.Abstractions;

/// <summary>
/// Server service abstraction for managing configured Discord server information in the database.
/// </summary>
public interface IServerService
    : IBaseService<ulong, Server, ServerReadDto, ServerCreateDto, ServerUpdateDto>
{
    /// <summary>
    /// Retrieves a server by its exact name.
    /// </summary>
    /// <param name="name">The name of the server.</param>
    /// <returns>The found server DTO, or <c>null</c> if not found.</returns>
    public Task<ServerReadDto?> GetByName(string name);
}
