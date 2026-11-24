using BusinessLogic.Enums.Types;
using BusinessLogic.Json.Models;
using BusinessLogic.Services.Abstractions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Voyager.API.Commands.Handlers;
using Voyager.API.Enums.Types;
using Voyager.API.Views;
using Voyager.API.Views.Components.Embeds;

namespace Voyager.API.Events;

/// <summary>
/// Central host for the bot's gateway event subscriptions. Wired up in
/// <c>Program.cs</c> on the singleton <see cref="DiscordClient"/>. Keeps the
/// per-event handler methods static so <c>DiscordClient.Event += ...</c>
/// reads cleanly at the call site; dependency resolution goes through the
/// <see cref="_scopeFactory"/> captured by <see cref="Initialize"/>.
/// </summary>
public static partial class EventHandler
{
    /// <summary>
    /// Captured once at startup so the static event handlers can open DI
    /// scopes to resolve scoped services (e.g. <see cref="IEnumService"/>).
    /// Call <see cref="Initialize"/> from Program.cs after the app is built.
    /// </summary>
    private static IServiceScopeFactory? _scopeFactory;

    /// <summary>
    /// Routing table from a parsed <see cref="InteractionIdType"/> to the
    /// async handler that should run for that component. The <c>None</c>
    /// entry throws on purpose so unrecognized custom_ids surface in the
    /// dispatcher's catch block as logged errors rather than silent no-ops.
    /// </summary>
    public static readonly Dictionary<InteractionIdType, Func<
        DiscordClient, ComponentInteractionCreateEventArgs, IServiceProvider, Task>>
        _interactionDispatchMap = new()
        {
            [InteractionIdType.None] = (sender, args, services) =>
                throw new ArgumentException("Interaction id does not exist"),
            [InteractionIdType.ClearAlert] = ClearAlert,
            [InteractionIdType.SwitchProfilePage] = SwitchProfilePage,
            [InteractionIdType.SaveRoll] = SaveRoll,
            [InteractionIdType.ClearRoll] = ClearRoll,
            [InteractionIdType.ConfirmDeleteCharacter] = ConfirmDeleteCharacter,
            [InteractionIdType.CancelDeleteCharacter] = CancelDeleteCharacter,
            [InteractionIdType.ConfirmDeleteGame] = ConfirmDeleteGame,
            [InteractionIdType.CancelDeleteGame] = CancelDeleteGame,
            [InteractionIdType.AcceptGameInvite] = AcceptGameInvite,
            [InteractionIdType.DeclineGameInvite] = DeclineGameInvite
        };

    /// <summary>
    /// Provides the static handlers with access to the DI container.
    /// Must be called exactly once at startup before any event fires.
    /// </summary>
    public static void Initialize(IServiceScopeFactory scopeFactory) =>
        _scopeFactory = scopeFactory;

    /// <summary>
    /// Fires once the gateway has finished its initial READY handshake.
    /// Placeholder — extend when startup-time work is needed.
    /// </summary>
    public static async Task OnReady(DiscordClient sender, ReadyEventArgs args)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Fires when the bot joins a new guild (or finishes resuming one).
    /// Placeholder — extend with onboarding/setup logic when needed.
    /// </summary>
    public static async Task GuildCreated(DiscordClient sender, GuildCreateEventArgs args)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Fires when the bot is removed from a guild or the guild becomes
    /// unavailable. Placeholder — extend with cleanup logic when needed.
    /// </summary>
    public static async Task GuildDeleted(DiscordClient sender, GuildDeleteEventArgs args)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Fires when a new member joins a guild the bot is in.
    /// Placeholder — extend with welcome / role-assignment logic.
    /// </summary>
    public static async Task GuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs args)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Fires when a member leaves (or is kicked/banned from) a guild.
    /// Placeholder — extend with audit / cleanup logic.
    /// </summary>
    public static async Task GuildMemberRemoved(DiscordClient sender, GuildMemberRemoveEventArgs args)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Dispatches a component interaction (button click, select menu, etc.)
    /// to the handler registered in <see cref="_interactionDispatchMap"/>. Any
    /// exception thrown by parsing or by the handler is caught here and
    /// logged in the same format as <c>SlashCommandErrored</c> in Program.cs
    /// so a bad component never silently breaks the bot.
    /// </summary>
    public static async Task ComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs args)
    {
        var customId = args.Interaction.Data.CustomId;
        await using var scope = _scopeFactory!.CreateAsyncScope();

        // Parse the custom_id → InteractionIdType. IEnumService is scoped,
        // so we open a short-lived scope just for the lookup.
        InteractionIdType type;
        try
        {
            var enumService = scope.ServiceProvider.GetRequiredService<IEnumService>();
            type = enumService.ConvertInteraction(customId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.UtcNow.ToLocalTime()}] Component interaction '{customId}' failed to parse: {ex}");
            return;
        }

        // Look up the handler. The None entry in _interactionMap throws on
        // purpose for unrecognized ids — that throw is caught below.
        if (!_interactionDispatchMap.TryGetValue(type, out var handler))
        {
            Console.WriteLine($"[{DateTime.UtcNow.ToLocalTime()}] Component interaction '{customId}' has no registered handler for {type}.");
            return;
        }

        try
        {
            await handler(sender, args, scope.ServiceProvider);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.UtcNow.ToLocalTime()}] Component interaction '{customId}' errored: {ex}");
        }
    }

    /// <summary>
    /// Handler for <see cref="InteractionIdType.ClearAlert"/>: deletes the
    /// message the dismiss button was attached to. Doesn't use
    /// <paramref name="services"/> because no scoped service is needed —
    /// it just calls the Discord REST API.
    /// </summary>
    private static async Task ClearAlert(DiscordClient sender, ComponentInteractionCreateEventArgs args,
        IServiceProvider services)
    {
        await args.Message.DeleteAsync();
    }

    /// <summary>
    /// Handler for <see cref="InteractionIdType.SwitchProfilePage"/>: reads
    /// the selected <see cref="ProfilePage"/> option from the dropdown on a
    /// user's own profile card and updates the message in-place with either
    /// the Profile view or the Settings view.
    /// </summary>
    private static async Task SwitchProfilePage(DiscordClient sender, ComponentInteractionCreateEventArgs args,
        IServiceProvider services)
    {
        var selectedValue = args.Values.FirstOrDefault();
        if (!Enum.TryParse<ProfilePage>(selectedValue, ignoreCase: true, out var targetPage))
        {
            throw new ArgumentException($"Unknown profile page selection '{selectedValue}'.");
        }

        var userService = services.GetRequiredService<IUserService>();
        var enumService = services.GetRequiredService<IEnumService>();
        var characterService = services.GetService<ICharacterService>();
        var gameService = services.GetService<IGameService>();
        bool isEphemeral = args.Guild is not null;

        var viewFactory = new ViewFactory(isEphemeral, enumService, new ServerSettings());
        var handler = new ProfileHandler(userService, enumService, viewFactory, characterService, gameService);

        DiscordMessageBuilder message = targetPage switch
        {
            ProfilePage.Profile => await handler.ViewAsync(args.User.Id, args.User.Id),
            ProfilePage.Settings => await handler.SettingsAsync(args.User.Id),
            _ => throw new ArgumentOutOfRangeException(nameof(targetPage), targetPage, "Unsupported profile page.")
        };

        await args.Interaction.CreateResponseAsync(
            InteractionResponseType.UpdateMessage,
            new DiscordInteractionResponseBuilder(message));
    }

    /// <summary>
    /// Evaluates whether a caller is authorized to clear a dice roll message:
    /// returns <c>true</c> if the caller is the original roller, the guild owner,
    /// a member with <see cref="Permissions.Administrator"/>, or a member holding
    /// the server's configured Game Master role (<paramref name="gmRoleId"/>).
    /// Extracted as a public static method so the authorization logic can be
    /// unit-tested directly with primitives.
    /// </summary>
    /// <param name="callerId">The Discord snowflake ID of the user clicking Clear.</param>
    /// <param name="rollerId">The Discord snowflake ID of the user who rolled the dice.</param>
    /// <param name="isServerOwner">Whether the caller is the Discord server owner.</param>
    /// <param name="isAdmin">Whether the caller has Discord Administrator permissions.</param>
    /// <param name="gmRoleId">Optional configured GM role ID for the server.</param>
    /// <param name="callerRoleIds">The Discord role IDs held by the caller in the guild.</param>
    public static bool CanClearRoll(
        ulong callerId,
        ulong rollerId,
        bool isServerOwner,
        bool isAdmin,
        ulong? gmRoleId,
        IEnumerable<ulong>? callerRoleIds)
    {
        if (callerId == rollerId || isServerOwner || isAdmin)
        {
            return true;
        }

        if (gmRoleId.HasValue && callerRoleIds is not null && callerRoleIds.Contains(gmRoleId.Value))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Handler for <see cref="InteractionIdType.ClearRoll"/>: allows only the
    /// person who rolled, a server GM, or a server Admin/Owner to delete the
    /// dice roll message. Unauthorized callers receive an ephemeral error notification.
    /// </summary>
    private static async Task ClearRoll(
        DiscordClient sender,
        ComponentInteractionCreateEventArgs args,
        IServiceProvider services)
    {
        var embed = args.Message.Embeds.FirstOrDefault();
        Embeds.TryExtractRollerIdFromDiceRollEmbed(embed, out ulong rollerId);

        bool isServerOwner = false;
        bool isAdmin = false;
        ulong? gmRoleId = null;
        IEnumerable<ulong>? callerRoleIds = null;
        ServerSettings serverSettings = new();

        if (args.Guild is not null)
        {
            isServerOwner = args.Guild.OwnerId == args.User.Id;

            if (args.User is DiscordMember member || args.Guild.Members.TryGetValue(args.User.Id, out member!))
            {
                isAdmin = (member.Permissions & Permissions.Administrator) != 0;
                callerRoleIds = member.Roles.Select(r => r.Id);
            }

            var serverService = services.GetRequiredService<IServerService>();
            var server = await serverService.GetByIdAsync(args.Guild.Id);
            if (server is not null)
            {
                gmRoleId = server.GMRoleId;
                serverSettings = server.Settings;
            }
        }

        if (!CanClearRoll(args.User.Id, rollerId, isServerOwner, isAdmin, gmRoleId, callerRoleIds))
        {
            var enumService = services.GetRequiredService<IEnumService>();
            var viewFactory = new ViewFactory(isEphemeral: true, enumService, serverSettings);
            var errorNotification = viewFactory.CreateNotification(
                NotificationType.Error,
                "Error: Only GMs, admins, or the person who rolled can clear this roll.");

            await args.Interaction.CreateResponseAsync(
                InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder(errorNotification).AsEphemeral(true));
            return;
        }

        await args.Message.DeleteAsync();
    }

    /// <summary>
    /// Evaluates whether a caller is authorized to confirm or cancel character deletion:
    /// only the owner of the character is allowed to interact with the confirmation prompt.
    /// </summary>
    /// <param name="callerId">The Discord snowflake ID of the user clicking Confirm or Cancel.</param>
    /// <param name="ownerUserId">The Discord snowflake ID of the character's owner.</param>
    public static bool CanInteractWithCharacterDeletion(ulong callerId, ulong ownerUserId) =>
        callerId == ownerUserId;

    /// <summary>
    /// Handler for <see cref="InteractionIdType.ConfirmDeleteCharacter"/>: permanently deletes
    /// the character and removes their stored image assets. Only the owner who initiated the
    /// command can confirm deletion; unauthorized callers receive an ephemeral error notification.
    /// </summary>
    private static async Task ConfirmDeleteCharacter(
        DiscordClient sender,
        ComponentInteractionCreateEventArgs args,
        IServiceProvider services)
    {
        var customId = args.Interaction.Data.CustomId;
        var parts = customId.Split(':');
        if (parts.Length < 3 ||
            !ulong.TryParse(parts[1], out var characterId) ||
            !ulong.TryParse(parts[2], out var ownerUserId))
        {
            Console.WriteLine($"[{DateTime.UtcNow.ToLocalTime()}] Invalid ConfirmDeleteCharacter custom ID: '{customId}'.");
            return;
        }

        var enumService = services.GetRequiredService<IEnumService>();

        if (!CanInteractWithCharacterDeletion(args.User.Id, ownerUserId))
        {
            var errorFactory = new ViewFactory(isEphemeral: true, enumService, new ServerSettings());
            var unauthorizedNotification = errorFactory.CreateNotification(
                NotificationType.Error,
                "You are not authorized to interact with this character deletion.");

            await args.Interaction.CreateResponseAsync(
                InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder(unauthorizedNotification).AsEphemeral(true));
            return;
        }

        var characterService = services.GetRequiredService<ICharacterService>();
        var fileService = services.GetRequiredService<IFileService>();
        var pathbuilderService = services.GetRequiredService<IPathbuilderService>();
        var userService = services.GetRequiredService<IUserService>();

        bool isEphemeral = args.Guild is not null;
        var viewFactory = new ViewFactory(isEphemeral, enumService, new ServerSettings());
        var handler = new CharacterHandler(
            characterService,
            userService,
            pathbuilderService,
            fileService,
            enumService,
            viewFactory);

        var message = await handler.ConfirmDeleteAsync(args.User.Id, characterId, ownerUserId);

        await args.Interaction.CreateResponseAsync(
            InteractionResponseType.UpdateMessage,
            new DiscordInteractionResponseBuilder(message));
    }

    /// <summary>
    /// Handler for <see cref="InteractionIdType.CancelDeleteCharacter"/>: cancels the character
    /// deletion and dismisses the confirmation message. Only the owner who initiated the
    /// command can cancel; unauthorized callers receive an ephemeral error notification.
    /// </summary>
    private static async Task CancelDeleteCharacter(
        DiscordClient sender,
        ComponentInteractionCreateEventArgs args,
        IServiceProvider services)
    {
        var customId = args.Interaction.Data.CustomId;
        var parts = customId.Split(':');
        if (parts.Length < 3 ||
            !ulong.TryParse(parts[1], out var characterId) ||
            !ulong.TryParse(parts[2], out var ownerUserId))
        {
            Console.WriteLine($"[{DateTime.UtcNow.ToLocalTime()}] Invalid CancelDeleteCharacter custom ID: '{customId}'.");
            return;
        }

        if (!CanInteractWithCharacterDeletion(args.User.Id, ownerUserId))
        {
            var enumService = services.GetRequiredService<IEnumService>();
            var errorFactory = new ViewFactory(isEphemeral: true, enumService, new ServerSettings());
            var unauthorizedNotification = errorFactory.CreateNotification(
                NotificationType.Error,
                "You are not authorized to interact with this character deletion.");

            await args.Interaction.CreateResponseAsync(
                InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder(unauthorizedNotification).AsEphemeral(true));
            return;
        }

        await args.Message.DeleteAsync();
    }

    /// <summary>
    /// Evaluates whether a caller is authorized to confirm or cancel game deletion:
    /// only the user who initiated the <c>/game delete</c> prompt is allowed to interact with it.
    /// </summary>
    /// <param name="callerId">The Discord snowflake ID of the user clicking Confirm or Cancel.</param>
    /// <param name="authorizedUserId">The Discord snowflake ID of the user who initiated deletion.</param>
    public static bool CanInteractWithGameDeletion(ulong callerId, ulong authorizedUserId) =>
        callerId == authorizedUserId;

    /// <summary>
    /// Handler for <see cref="InteractionIdType.ConfirmDeleteGame"/>: archives the game's channels,
    /// soft-deletes the game in the database, clears <c>CurrentGame</c> on associated characters,
    /// and removes stored image assets.
    /// </summary>
    private static async Task ConfirmDeleteGame(
        DiscordClient sender,
        ComponentInteractionCreateEventArgs args,
        IServiceProvider services)
    {
        var customId = args.Interaction.Data.CustomId;
        var parts = customId.Split(':');
        if (parts.Length < 3 ||
            !ulong.TryParse(parts[1], out var gameId) ||
            !ulong.TryParse(parts[2], out var authorizedUserId))
        {
            Console.WriteLine($"[{DateTime.UtcNow.ToLocalTime()}] Invalid ConfirmDeleteGame custom ID: '{customId}'.");
            return;
        }

        var enumService = services.GetRequiredService<IEnumService>();

        if (!CanInteractWithGameDeletion(args.User.Id, authorizedUserId))
        {
            var errorFactory = new ViewFactory(isEphemeral: true, enumService, new ServerSettings());
            var unauthorizedNotification = errorFactory.CreateNotification(
                NotificationType.Error,
                "You are not authorized to interact with this game deletion.");

            await args.Interaction.CreateResponseAsync(
                InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder(unauthorizedNotification).AsEphemeral(true));
            return;
        }

        await args.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

        var gameService = services.GetRequiredService<IGameService>();
        var serverService = services.GetRequiredService<IServerService>();
        var userService = services.GetRequiredService<IUserService>();
        var fileService = services.GetRequiredService<IFileService>();

        var viewFactory = new ViewFactory(isEphemeral: false, enumService, new ServerSettings());
        var handler = new GameHandler(
            gameService,
            serverService,
            userService,
            fileService,
            enumService,
            viewFactory);

        var message = await handler.ConfirmDeleteAsync(args.Guild, args.User.Id, gameId, authorizedUserId);
        await args.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder(message));
    }

    /// <summary>
    /// Handler for <see cref="InteractionIdType.CancelDeleteGame"/>: cancels the game
    /// deletion and dismisses the confirmation message.
    /// </summary>
    private static async Task CancelDeleteGame(
        DiscordClient sender,
        ComponentInteractionCreateEventArgs args,
        IServiceProvider services)
    {
        var customId = args.Interaction.Data.CustomId;
        var parts = customId.Split(':');
        if (parts.Length < 3 ||
            !ulong.TryParse(parts[1], out var gameId) ||
            !ulong.TryParse(parts[2], out var authorizedUserId))
        {
            Console.WriteLine($"[{DateTime.UtcNow.ToLocalTime()}] Invalid CancelDeleteGame custom ID: '{customId}'.");
            return;
        }

        if (!CanInteractWithGameDeletion(args.User.Id, authorizedUserId))
        {
            var enumService = services.GetRequiredService<IEnumService>();
            var errorFactory = new ViewFactory(isEphemeral: true, enumService, new ServerSettings());
            var unauthorizedNotification = errorFactory.CreateNotification(
                NotificationType.Error,
                "You are not authorized to interact with this game deletion.");

            await args.Interaction.CreateResponseAsync(
                InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder(unauthorizedNotification).AsEphemeral(true));
            return;
        }

        await args.Message.DeleteAsync();
    }

    /// <summary>
    /// Handler for <see cref="InteractionIdType.AcceptGameInvite"/>: enrolls the invited player
    /// in the game, updates the DM message, and notifies the game's <c>gm-channel</c>.
    /// </summary>
    private static Task AcceptGameInvite(
        DiscordClient sender,
        ComponentInteractionCreateEventArgs args,
        IServiceProvider services) =>
        HandleGameInviteResponseAsync(sender, args, services, accepted: true);

    /// <summary>
    /// Handler for <see cref="InteractionIdType.DeclineGameInvite"/>: declines the game invitation,
    /// updates the DM message, and notifies the game's <c>gm-channel</c>.
    /// </summary>
    private static Task DeclineGameInvite(
        DiscordClient sender,
        ComponentInteractionCreateEventArgs args,
        IServiceProvider services) =>
        HandleGameInviteResponseAsync(sender, args, services, accepted: false);

    private static async Task HandleGameInviteResponseAsync(
        DiscordClient sender,
        ComponentInteractionCreateEventArgs args,
        IServiceProvider services,
        bool accepted)
    {
        var customId = args.Interaction.Data.CustomId;
        var parts = customId.Split(':');
        if (parts.Length < 3 ||
            !ulong.TryParse(parts[1], out var gameId) ||
            !ulong.TryParse(parts[2], out var invitedUserId))
        {
            Console.WriteLine($"[{DateTime.UtcNow.ToLocalTime()}] Invalid GameInvite custom ID: '{customId}'.");
            return;
        }

        var enumService = services.GetRequiredService<IEnumService>();

        if (args.User.Id != invitedUserId)
        {
            var errorFactory = new ViewFactory(isEphemeral: true, enumService, new ServerSettings());
            var unauthorizedNotification = errorFactory.CreateNotification(
                NotificationType.Error,
                "This game invitation is not for you.");

            await args.Interaction.CreateResponseAsync(
                InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder(unauthorizedNotification).AsEphemeral(true));
            return;
        }

        var gameService = services.GetRequiredService<IGameService>();
        var serverService = services.GetRequiredService<IServerService>();
        var userService = services.GetRequiredService<IUserService>();
        var fileService = services.GetRequiredService<IFileService>();

        var viewFactory = new ViewFactory(isEphemeral: false, enumService, new ServerSettings());
        var handler = new GameHandler(
            gameService,
            serverService,
            userService,
            fileService,
            enumService,
            viewFactory);

        var (dmResponse, gmChannelId, gmNotification) = await handler.HandleInviteResponseAsync(
            args.User.Id,
            args.User.Username,
            gameId,
            invitedUserId,
            accepted);

        await args.Interaction.CreateResponseAsync(
            InteractionResponseType.UpdateMessage,
            new DiscordInteractionResponseBuilder(dmResponse));

        if (gmChannelId.HasValue && gmNotification is not null)
        {
            try
            {
                var gmChannel = await sender.GetChannelAsync(gmChannelId.Value);
                await gmChannel.SendMessageAsync(gmNotification);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.UtcNow.ToLocalTime()}] Failed to send invite response notification to gm-channel {gmChannelId}: {ex}");
            }
        }
    }

    /// <summary>
    /// Handler for <see cref="InteractionIdType.SaveRoll"/>: extracts the dice
    /// formula from the roll embed, checks if the user already saved it or reached
    /// the 25-roll limit, and if valid prompts the user with a modal to name the
    /// saved dice configuration.
    /// </summary>
    private static async Task SaveRoll(
        DiscordClient sender,
        ComponentInteractionCreateEventArgs args,
        IServiceProvider services)
    {
        var diceRollService = services.GetRequiredService<IDiceRollService>();
        var userService = services.GetRequiredService<IUserService>();
        var serverService = services.GetRequiredService<IServerService>();
        var enumService = services.GetRequiredService<IEnumService>();

        ServerSettings serverSettings = new();
        if (args.Guild is not null)
        {
            var server = await serverService.GetByIdAsync(args.Guild.Id);
            if (server is not null)
            {
                serverSettings = server.Settings;
            }
        }

        bool isEphemeral = args.Guild is not null;
        var viewFactory = new ViewFactory(isEphemeral, enumService, serverSettings);
        var handler = new DiceHandler(diceRollService, userService, viewFactory);

        var embed = args.Message.Embeds.FirstOrDefault();
        string? formula = Embeds.ExtractFormulaFromDiceRollEmbed(embed);

        var validationError = await handler.ValidateCanSaveAsync(args.User.Id, formula);
        if (validationError is not null)
        {
            await args.Interaction.CreateResponseAsync(
                InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder(validationError).AsEphemeral(isEphemeral));
            return;
        }

        var modal = new DiscordInteractionResponseBuilder()
            .WithTitle("Save Dice Configuration")
            .WithCustomId(enumService.ConvertInteraction(InteractionIdType.SaveRollModal))
            .AddComponents(new TextInputComponent(
                label: "Configuration Name",
                customId: "dice_name",
                placeholder: "e.g. Fireball Damage",
                required: true,
                style: TextInputStyle.Short,
                min_length: 1,
                max_length: 100))
            .AddComponents(new TextInputComponent(
                label: "Dice Formula",
                customId: "dice_formula",
                value: formula,
                required: true,
                style: TextInputStyle.Short,
                min_length: 1,
                max_length: 200));

        await args.Interaction.CreateResponseAsync(InteractionResponseType.Modal, modal);
    }

    /// <summary>
    /// Dispatches modal submission interactions (e.g. <see cref="InteractionIdType.SaveRollModal"/>).
    /// </summary>
    public static async Task ModalSubmitted(DiscordClient sender, ModalSubmitEventArgs args)
    {
        var customId = args.Interaction.Data.CustomId;
        await using var scope = _scopeFactory!.CreateAsyncScope();

        InteractionIdType type;
        try
        {
            var enumService = scope.ServiceProvider.GetRequiredService<IEnumService>();
            type = enumService.ConvertInteraction(customId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.UtcNow.ToLocalTime()}] Modal interaction '{customId}' failed to parse: {ex}");
            return;
        }

        try
        {
            if (type == InteractionIdType.SaveRollModal)
            {
                await HandleSaveRollModalAsync(args, scope.ServiceProvider);
            }
            else if (type == InteractionIdType.ServerChangeModal)
            {
                await HandleServerChangeModalAsync(args, scope.ServiceProvider);
            }
            else
            {
                Console.WriteLine($"[{DateTime.UtcNow.ToLocalTime()}] Modal interaction '{customId}' has no registered handler for {type}.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.UtcNow.ToLocalTime()}] Modal interaction '{customId}' errored: {ex}");
        }
    }

    private static async Task HandleSaveRollModalAsync(ModalSubmitEventArgs args, IServiceProvider services)
    {
        var diceRollService = services.GetRequiredService<IDiceRollService>();
        var userService = services.GetRequiredService<IUserService>();
        var serverService = services.GetRequiredService<IServerService>();
        var enumService = services.GetRequiredService<IEnumService>();

        ServerSettings serverSettings = new();
        if (args.Interaction.Guild is not null)
        {
            var server = await serverService.GetByIdAsync(args.Interaction.Guild.Id);
            if (server is not null)
            {
                serverSettings = server.Settings;
            }
        }

        bool isEphemeral = args.Interaction.Guild is not null;
        var viewFactory = new ViewFactory(isEphemeral, enumService, serverSettings);
        var handler = new DiceHandler(diceRollService, userService, viewFactory);

        args.Values.TryGetValue("dice_name", out string? name);
        args.Values.TryGetValue("dice_formula", out string? formula);

        var message = await handler.CreateAsync(args.Interaction.User.Id, name ?? string.Empty, formula ?? string.Empty);

        await args.Interaction.CreateResponseAsync(
            InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder(message).AsEphemeral(isEphemeral));
    }

    private static async Task HandleServerChangeModalAsync(ModalSubmitEventArgs args, IServiceProvider services)
    {
        var serverService = services.GetRequiredService<IServerService>();
        var enumService = services.GetRequiredService<IEnumService>();

        if (args.Interaction.Guild is null)
        {
            return;
        }

        ulong guildId = args.Interaction.Guild.Id;

        ulong? gmRoleId = null;
        ulong? adminChannelId = null;
        var parts = args.Interaction.Data.CustomId.Split(':');
        if (parts.Length >= 3)
        {
            if (ulong.TryParse(parts[1], out var parsedGm) && parsedGm != 0)
            {
                gmRoleId = parsedGm;
            }
            if (ulong.TryParse(parts[2], out var parsedAdmin) && parsedAdmin != 0)
            {
                adminChannelId = parsedAdmin;
            }
        }

        args.Values.TryGetValue("server_description", out string? description);

        string? generalColor = null;
        string? errorColor = null;
        string? successColor = null;
        string? warningColor = null;

        if (args.Values.TryGetValue("server_colors", out string? combinedColors) && !string.IsNullOrWhiteSpace(combinedColors))
        {
            (generalColor, errorColor, successColor, warningColor) = ServerHandler.ParseCombinedColors(combinedColors);
        }
        else
        {
            args.Values.TryGetValue("server_general_color", out generalColor);
            args.Values.TryGetValue("server_error_color", out errorColor);
            args.Values.TryGetValue("server_success_color", out successColor);
            args.Values.TryGetValue("server_warning_color", out warningColor);
        }

        var viewFactory = new ViewFactory(isEphemeral: true, enumService, new ServerSettings());
        var handler = new ServerHandler(serverService, enumService, viewFactory);

        var message = await handler.ChangeAsync(
            guildId,
            gmRoleId,
            adminChannelId,
            description,
            generalColor,
            errorColor,
            successColor,
            warningColor);

        await args.Interaction.CreateResponseAsync(
            InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder(message).AsEphemeral(true));
    }
}
