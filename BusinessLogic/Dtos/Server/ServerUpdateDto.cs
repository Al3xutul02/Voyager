using BusinessLogic.Json.Models;

namespace BusinessLogic.Dtos.Server;

/// <summary>
/// Data transfer object for updating an existing Discord server record.
/// </summary>
/// <param name="Id">The Discord snowflake ID of the guild to update.</param>
/// <param name="Name">The updated display name of the Discord guild.</param>
/// <param name="Description">The updated optional description for the server's profile card.</param>
/// <param name="GMRoleId">The updated Discord snowflake ID of the server's Game Master role.</param>
/// <param name="AdminChannelId">The updated Discord snowflake ID of the server's Voyager admin channel.</param>
/// <param name="Settings">The updated <see cref="ServerSettings"/> to serialize and store.</param>
public record ServerUpdateDto(
    ulong Id,
    string Name,
    string? Description,
    ulong GMRoleId,
    ulong AdminChannelId,
    ServerSettings Settings);
