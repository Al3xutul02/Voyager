using BusinessLogic.Enums.Types;

namespace BusinessLogic.Dtos.Game;

/// <summary>
/// Data transfer object for creating a new game on a server in Normal mode.
/// </summary>
/// <param name="ServerId">The Discord snowflake ID of the hosting server.</param>
/// <param name="GameMasterId">The Discord snowflake ID of the Game Master who created the game.</param>
/// <param name="Name">The name of the game.</param>
/// <param name="Description">The description or synopsis of the game.</param>
/// <param name="Color">Palette color used for the game's embeds.</param>
/// <param name="GmTokenImagePath">Relative path on disk where the GM token image is stored.</param>
/// <param name="ThumbnailImagePath">Relative path on disk where the game's thumbnail image is stored.</param>
/// <param name="CategoryId">Discord snowflake ID of the game's channel category.</param>
/// <param name="GmChannelId">Discord snowflake ID of the game's private <c>gm-channel</c>.</param>
/// <param name="GeneralChannelId">Discord snowflake ID of the game's <c>general</c> channel.</param>
/// <param name="CharactersChannelId">Discord snowflake ID of the game's <c>characters</c> channel.</param>
/// <param name="NpcsChannelId">Discord snowflake ID of the game's private <c>npcs</c> channel.</param>
public record GameCreateDto(
    ulong ServerId,
    ulong GameMasterId,
    string Name,
    string Description,
    Color Color,
    string GmTokenImagePath,
    string ThumbnailImagePath,
    ulong CategoryId,
    ulong GmChannelId,
    ulong GeneralChannelId,
    ulong CharactersChannelId,
    ulong NpcsChannelId
);
