using BusinessLogic.Json.Models;

namespace BusinessLogic.Dtos.Server;

public record ServerUpdateDto(
    ulong Id,
    string Name,
    string? Description,
    ulong GMRoleId,
    ulong AdminChannelId,
    ServerSettings Settings);
