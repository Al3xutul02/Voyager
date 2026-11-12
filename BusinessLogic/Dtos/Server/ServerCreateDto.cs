using BusinessLogic.Json.Models;

namespace BusinessLogic.Dtos.Server;

public record ServerCreateDto(ulong Id, string Name, ServerSettings Settings);
