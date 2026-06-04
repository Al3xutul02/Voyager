using BusinessLogic.Json.Models;

namespace BusinessLogic.Dtos.User;

/// <summary>
/// Data transfer object for user read information.
/// </summary>
public record UserReadDto(ulong Id, string Name, UserSettings Settings);
