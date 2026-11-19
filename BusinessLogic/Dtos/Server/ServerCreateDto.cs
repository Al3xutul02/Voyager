using BusinessLogic.Json.Models;

namespace BusinessLogic.Dtos.Server;

public record ServerCreateDto(
    ulong Id,
    string Name,
    ulong GMRoleId,
    ulong AdminChannelId,
    ServerSettings Settings);
