using BusinessLogic.Json.Models;

namespace BusinessLogic.Dtos.Server;

/// <summary>
/// Data transfer object for creating a new configured Discord server record.
/// </summary>
/// <param name="Id">The Discord snowflake ID of the guild.</param>
/// <param name="Name">The display name of the Discord guild.</param>
/// <param name="GMRoleId">The Discord snowflake ID of the server's Game Master role.</param>
/// <param name="AdminChannelId">The Discord snowflake ID of the server's Voyager admin channel.</param>
/// <param name="Settings">The initial server settings (operating mode and notification palette colors).</param>
public record ServerCreateDto(
    ulong Id,
    string Name,
    ulong GMRoleId,
    ulong AdminChannelId,
    ServerSettings Settings);
