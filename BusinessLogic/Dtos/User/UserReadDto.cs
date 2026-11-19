using BusinessLogic.Json.Models;

namespace BusinessLogic.Dtos.User;

/// <summary>
/// Data transfer object for user read information.
/// </summary>
/// <param name="Id">The Discord snowflake ID of the user.</param>
/// <param name="Name">The Discord username of the user.</param>
/// <param name="Settings">The user's deserialized <see cref="UserSettings"/>.</param>
public record UserReadDto(ulong Id, string Name, UserSettings Settings);
