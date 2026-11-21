using BusinessLogic.Dtos.Character;
using BusinessLogic.Dtos.DiceRoll;
using BusinessLogic.Dtos.Server;
using BusinessLogic.Dtos.User;
using BusinessLogic.Enums.Types;
using BusinessLogic.Json.Models;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;
using Voyager.API.Enums.Types;
using Voyager.API.Views.Components.Buttons;
using Voyager.API.Views.Components.Embeds;
using Voyager.API.Views.Components.Selects;

namespace Voyager.API.Views;

/// <summary>
/// Assembles complete <see cref="DiscordMessageBuilder"/> responses out of
/// the reusable building blocks in <c>Views.Components</c>. Slash commands
/// and interaction handlers depend on this rather than constructing
/// embeds/buttons/selects inline.
/// </summary>
/// <param name="isEphemeral">
/// Whether the message is sent ephemerally (in a server). When <c>false</c>
/// (e.g. in DMs), a manual "Clear" button is appended so the user can dismiss
/// the bot's message.
/// </param>
/// <param name="enumService">Used by every view that needs color or interaction-id conversion.</param>
/// <param name="settings">Server-wide UI defaults (colors for notifications, etc.).</param>
public class ViewFactory(
    bool isEphemeral,
    IEnumService enumService,
    ServerSettings settings)
{
    public ServerSettings Settings
    {
        get => settings ?? throw new ArgumentNullException(nameof(settings), "ServerSettings cannot be null when constructing a ViewFactory.");

        set => settings = value ?? throw new ArgumentNullException(nameof(settings), "ServerSettings cannot be null when constructing a ViewFactory.");
    }

    private readonly bool _isEphemeral = isEphemeral;
    private readonly IEnumService _enumService = enumService;

    /// <summary>
    /// Builds a notification message: a colored embed plus an optional body
    /// description and a "Clear" dismissal button when non-ephemeral.
    /// </summary>
    /// <param name="notificationType">Picks which color from <see cref="ServerSettings"/> is used.</param>
    /// <param name="message">Text rendered as the embed title.</param>
    /// <param name="description">Optional detailed text rendered in the embed description body.</param>
    public DiscordMessageBuilder CreateNotification(NotificationType notificationType, string message, string? description = null)
    {
        var messageBuilder = new DiscordMessageBuilder()
            .AddEmbed(Embeds.Notification(_enumService, Settings, notificationType, message, description));

        if (!_isEphemeral)
            messageBuilder.AddComponents(Buttons.ClearMessage(_enumService, "Clear this notification"));

        return messageBuilder;
    }

    /// <summary>
    /// Builds the user-profile message used by <c>/profile view</c>:
    /// the profile embed, a page-switcher dropdown if the caller is viewing
    /// their own profile, and a "Clear" dismissal button when non-ephemeral.
    /// </summary>
    /// <param name="userDto">The user whose profile is being rendered.</param>
    /// <param name="callerId">The Discord user ID of the person who invoked the command.</param>
    /// <param name="characters">Optional collection of active characters owned by the user.</param>
    public DiscordMessageBuilder CreateUserProfile(
        UserReadDto userDto,
        ulong callerId,
        IEnumerable<CharacterReadDto>? characters = null)
    {
        var messageBuilder = new DiscordMessageBuilder()
            .AddEmbed(Embeds.UserProfile(_enumService, userDto, callerId, characters));

        if (callerId == userDto.Id)
            messageBuilder.AddComponents(Selects.SwitchProfilePage(_enumService, ProfilePage.Profile));

        if (!_isEphemeral)
            messageBuilder.AddComponents(Buttons.ClearMessage(_enumService, "Clear"));

        return messageBuilder;
    }

    /// <summary>
    /// Builds the character sheet message used by <c>/character details</c>:
    /// attaches the token and actor portrait images, renders the character details embed,
    /// and appends a "Clear" button when non-ephemeral.
    /// </summary>
    /// <param name="character">The character DTO to display.</param>
    /// <param name="playerName">The username of the owning player.</param>
    /// <param name="tokenStream">Optional stream for token image.</param>
    /// <param name="actorStream">Optional stream for actor portrait image.</param>
    public DiscordMessageBuilder CreateCharacterDetails(
        CharacterReadDto character,
        string playerName,
        Stream? tokenStream = null,
        Stream? actorStream = null)
    {
        var messageBuilder = new DiscordMessageBuilder();

        if (tokenStream is not null)
        {
            messageBuilder.AddFile("token.png", tokenStream);
        }

        if (actorStream is not null)
        {
            messageBuilder.AddFile("actor.png", actorStream);
        }

        messageBuilder.AddEmbed(Embeds.CharacterDetails(
            _enumService,
            character,
            playerName,
            hasTokenAttachment: tokenStream is not null,
            hasActorAttachment: actorStream is not null));

        if (!_isEphemeral)
        {
            messageBuilder.AddComponents(Buttons.ClearMessage(_enumService, "Clear"));
        }

        return messageBuilder;
    }

    /// <summary>
    /// Builds the confirmation warning prompt message for <c>/character delete</c>:
    /// the warning embed plus "Confirm Delete" and "Cancel" buttons restricted to the owner.
    /// </summary>
    /// <param name="character">The character to be deleted.</param>
    /// <param name="callerId">The Discord snowflake ID of the calling user.</param>
    public DiscordMessageBuilder CreateCharacterDeleteConfirmation(
        CharacterReadDto character,
        ulong callerId)
    {
        return new DiscordMessageBuilder()
            .AddEmbed(Embeds.CharacterDeleteConfirmation(_enumService, Settings, character.Name))
            .AddComponents(
                Buttons.ConfirmDeleteCharacter(_enumService, character.Id, callerId),
                Buttons.CancelDeleteCharacter(_enumService, character.Id, callerId));
    }

    /// <summary>
    /// Builds the user-settings message shown when a user switches their
    /// profile card to the Settings page: the settings embed, the page-switcher
    /// dropdown pre-selected to <see cref="ProfilePage.Settings"/>, and a
    /// "Clear" dismissal button when non-ephemeral.
    /// </summary>
    /// <param name="userDto">The user whose settings are being rendered.</param>
    public DiscordMessageBuilder CreateUserSettings(UserReadDto userDto)
    {
        var messageBuilder = new DiscordMessageBuilder()
            .AddEmbed(Embeds.UserSettings(_enumService, userDto))
            .AddComponents(Selects.SwitchProfilePage(_enumService, ProfilePage.Settings));

        if (!_isEphemeral)
            messageBuilder.AddComponents(Buttons.ClearMessage(_enumService, "Clear"));

        return messageBuilder;
    }

    /// <summary>
    /// Builds the server-profile message used by <c>/server view</c> and
    /// <c>/server setup</c>: the server profile embed plus a "Clear"
    /// dismissal button when non-ephemeral.
    /// </summary>
    /// <param name="server">The server DTO whose profile is being rendered.</param>
    public DiscordMessageBuilder CreateServerProfile(ServerReadDto server)
    {
        var messageBuilder = new DiscordMessageBuilder()
            .AddEmbed(Embeds.ServerProfile(_enumService, server));

        if (!_isEphemeral)
            messageBuilder.AddComponents(Buttons.ClearMessage(_enumService, "Clear"));
        return messageBuilder;
    }

    /// <summary>
    /// Builds the dice roll result or statistics message used by <c>/dice roll</c>:
    /// the dice roll embed plus a row containing the "Save" button and the
    /// restricted "Clear" button.
    /// </summary>
    /// <param name="callerId">The Discord snowflake ID of the user who rolled.</param>
    /// <param name="callerName">The Discord username of the user who rolled.</param>
    /// <param name="evaluation">The evaluated dice roll result and statistics.</param>
    /// <param name="color">Optional user profile color; defaults to <see cref="ServerSettings.GeneralColor"/> when omitted.</param>
    /// <param name="savedRollName">Optional name of the saved dice configuration if one was rolled.</param>
    public DiscordMessageBuilder CreateDiceRoll(
        ulong callerId,
        string callerName,
        DiceEvaluationResultDto evaluation,
        Color? color = null,
        string? savedRollName = null)
    {
        var embedColor = color ?? Settings.GeneralColor;
        return new DiscordMessageBuilder()
            .AddEmbed(Embeds.DiceRoll(_enumService, embedColor, callerId, callerName, evaluation, savedRollName))
            .AddComponents(
                Buttons.SaveRoll(_enumService, "Save"),
                Buttons.ClearRoll(_enumService, "Clear"));
    }

    /// <summary>
    /// Builds the dice roll syntax and rules guide message used by <c>/dice guide</c>:
    /// the guide embed plus a "Clear" dismissal button when non-ephemeral.
    /// </summary>
    public DiscordMessageBuilder CreateDiceGuide()
    {
        var messageBuilder = new DiscordMessageBuilder()
            .AddEmbed(Embeds.DiceGuide(_enumService, Settings));

        if (!_isEphemeral)
            messageBuilder.AddComponents(Buttons.ClearMessage(_enumService, "Clear"));

        return messageBuilder;
    }

    /// <summary>
    /// Builds the palette colors reference message used by <c>/server colors</c>:
    /// the colors embed plus a "Clear" dismissal button when non-ephemeral.
    /// </summary>
    public DiscordMessageBuilder CreateServerColors()
    {
        var messageBuilder = new DiscordMessageBuilder()
            .AddEmbed(Embeds.ServerColors(_enumService, Settings));

        if (!_isEphemeral)
            messageBuilder.AddComponents(Buttons.ClearMessage(_enumService, "Clear"));

        return messageBuilder;
    }
}
