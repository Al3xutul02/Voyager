using DSharpPlus.SlashCommands;

namespace Voyager.API.Commands;

[SlashCommandGroup("server", "Commands regarding the server")]
public class Server(IServiceScopeFactory scopeFactory) : ApplicationCommandModule
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
}
