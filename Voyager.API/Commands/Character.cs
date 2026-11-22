using BusinessLogic.Enums.Types;
using BusinessLogic.Json.Models;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Voyager.API.Commands.Autocompletes;
using Voyager.API.Commands.Handlers;
using Voyager.API.Views;

namespace Voyager.API.Commands;

/// <summary>
/// Slash command group for creating, viewing, editing, and deleting characters
/// (<c>/character create</c>, <c>/character details</c>, <c>/character delete</c>, <c>/character edit</c>).
/// </summary>
/// <param name="scopeFactory">Factory used to resolve scoped services per interaction.</param>
[SlashCommandGroup("character", "Commands for creating and managing characters")]
public class Character(IServiceScopeFactory scopeFactory) : ApplicationCommandModule
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    /// <summary>
    /// Creates a new character from Pathbuilder 2e build data (via export ID or uploaded JSON file) and uploaded images.
    /// </summary>
    [SlashCommand("create", "Create a new character imported from Pathbuilder 2e (via export ID or JSON file)")]
    public async Task Create(
        InteractionContext ctx,
        [Option("TokenImage", "Token image for the character")] DiscordAttachment tokenImage,
        [Option("ActorImage", "Actor portrait image for the character")] DiscordAttachment actorImage,
        [Autocomplete(typeof(ColorAutocompleteProvider))]
        [Option("Color", "Palette color for the character's embeds", true)] string color,
        [Option("PathbuilderId", "Pathbuilder 2e export ID or URL (provide this or JsonFile)")] string? pathbuilderId = null,
        [Option("JsonFile", "Pathbuilder 2e exported JSON file (provide this or PathbuilderId)")] DiscordAttachment? jsonFile = null,
        [Option("Description", "Optional character description or backstory")] string? description = null)
    {
        bool isServer = ctx.Guild is not null;
        await ctx.DeferAsync(ephemeral: isServer);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var handler = await CreateHandlerAsync(scope.ServiceProvider, ctx.Guild?.Id, isEphemeral: isServer);

        var message = await handler.CreateAsync(
            ctx.User.Id,
            ctx.User.Username,
            color,
            pathbuilderId,
            tokenImage.Url,
            tokenImage.FileName,
            actorImage.Url,
            actorImage.FileName,
            description,
            jsonFile?.Url);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder(message));
    }

    /// <summary>
    /// Displays the detailed character sheet embed for one of the caller's characters.
    /// </summary>
    [SlashCommand("details", "View details and character sheet of one of your characters")]
    public async Task Details(
        InteractionContext ctx,
        [Autocomplete(typeof(CharacterAutocompleteProvider))]
        [Option("Name", "Name of the character to view", true)] string name)
    {
        bool isServer = ctx.Guild is not null;
        await ctx.DeferAsync(ephemeral: isServer);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var handler = await CreateHandlerAsync(scope.ServiceProvider, ctx.Guild?.Id, isEphemeral: isServer);

        var message = await handler.DetailsAsync(ctx.User.Id, name, ctx.User.Username);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder(message));
    }

    /// <summary>
    /// Prompts the user with an irreversible deletion warning and a Confirm Delete button.
    /// </summary>
    [SlashCommand("delete", "Delete one of your characters")]
    public async Task Delete(
        InteractionContext ctx,
        [Autocomplete(typeof(CharacterAutocompleteProvider))]
        [Option("Name", "Name of the character to delete", true)] string name)
    {
        // Non-ephemeral so deletion confirmation is public in channel
        await ctx.DeferAsync(ephemeral: false);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var handler = await CreateHandlerAsync(scope.ServiceProvider, ctx.Guild?.Id, isEphemeral: false);

        var message = await handler.DeletePromptAsync(ctx.User.Id, name);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder(message));
    }

    /// <summary>
    /// Updates optional images, color, Pathbuilder data (via ID or JSON file), or description for an existing character.
    /// </summary>
    [SlashCommand("edit", "Edit or update a character's details or Pathbuilder build (via ID or JSON file)")]
    public async Task Edit(
        InteractionContext ctx,
        [Autocomplete(typeof(CharacterAutocompleteProvider))]
        [Option("Name", "Name of the character to edit", true)] string name,
        [Option("TokenImage", "Optional new token image")] DiscordAttachment? tokenImage = null,
        [Option("ActorImage", "Optional new actor portrait image")] DiscordAttachment? actorImage = null,
        [Autocomplete(typeof(ColorAutocompleteProvider))]
        [Option("Color", "Optional new palette color for the character", true)] string? color = null,
        [Option("PathbuilderId", "Optional Pathbuilder 2e export ID or URL to sync stats (or use JsonFile)")] string? pathbuilderId = null,
        [Option("JsonFile", "Optional Pathbuilder 2e exported JSON file to sync stats (or use PathbuilderId)")] DiscordAttachment? jsonFile = null,
        [Option("Description", "Optional new character description or backstory")] string? description = null)
    {
        bool isServer = ctx.Guild is not null;
        await ctx.DeferAsync(ephemeral: isServer);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var handler = await CreateHandlerAsync(scope.ServiceProvider, ctx.Guild?.Id, isEphemeral: isServer);

        var message = await handler.EditAsync(
            ctx.User.Id,
            name,
            tokenImage?.Url,
            tokenImage?.FileName,
            actorImage?.Url,
            actorImage?.FileName,
            color,
            pathbuilderId,
            description,
            jsonFile?.Url);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder(message));
    }

    private static async Task<CharacterHandler> CreateHandlerAsync(
        IServiceProvider services,
        ulong? guildId,
        bool isEphemeral)
    {
        var characterService = services.GetRequiredService<ICharacterService>();
        var userService = services.GetRequiredService<IUserService>();
        var serverService = services.GetRequiredService<IServerService>();
        var enumService = services.GetRequiredService<IEnumService>();
        var pathbuilderService = services.GetRequiredService<IPathbuilderService>();
        var fileService = services.GetRequiredService<IFileService>();

        ServerSettings settings = new();
        if (guildId.HasValue)
        {
            var server = await serverService.GetByIdAsync(guildId.Value);
            if (server is not null)
            {
                settings = server.Settings;
            }
        }

        var viewFactory = new ViewFactory(isEphemeral, enumService, settings);
        return new CharacterHandler(characterService, userService, pathbuilderService, fileService, enumService, viewFactory);
    }
}
