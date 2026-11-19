using BusinessLogic.Json.Models;

namespace BusinessLogic.Dtos.User;

/// <summary>
/// Data transfer object for user update information.
/// </summary>
/// <param name="Id">The Discord snowflake ID of the user to update.</param>
/// <param name="Name">The updated Discord username of the user.</param>
/// <param name="Settings">The updated <see cref="UserSettings"/> to serialize and store.</param>
public record UserUpdateDto(ulong Id, string Name, UserSettings Settings);
