using BusinessLogic.Json.Models;

namespace BusinessLogic.Dtos.User;

/// <summary>
/// Data transfer object for user update information.
/// </summary>
public record UserUpdateDto(ulong Id, string Name, UserSettings Settings);