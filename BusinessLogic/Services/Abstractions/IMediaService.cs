using BusinessLogic.Enums.Types;
using DSharpPlus.Entities;

namespace BusinessLogic.Services.Abstractions;

public interface IMediaSerivce
{
    public Color ConvertColor(DiscordColor color);
    public DiscordColor ConvertColor(Color color);
}
