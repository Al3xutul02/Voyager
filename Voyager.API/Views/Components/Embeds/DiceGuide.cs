using BusinessLogic.Json.Models;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;

namespace Voyager.API.Views.Components.Embeds;

public static partial class Embeds
{
    /// <summary>
    /// Builds the dice syntax and rules guide embed shown by <c>/dice guide</c>.
    /// </summary>
    /// <param name="enumService">Maps the general palette color from <paramref name="settings"/> to a <see cref="DiscordColor"/>.</param>
    /// <param name="settings">Server-wide color defaults.</param>
    public static DiscordEmbedBuilder DiceGuide(IEnumService enumService, ServerSettings settings)
    {
        return new DiscordEmbedBuilder
        {
            Title = "🎲 Voyager Dice Roll Guide",
            Description = "Use `/dice roll` to roll saved configurations or custom dice formulas, `/dice create` to save a configuration, and `/dice delete` to remove one.",
            Color = enumService.ConvertColor(settings.GeneralColor)
        }
        .AddField(
            "**Basic Syntax & Modifiers**",
            "• `NdS` — Roll `N` dice with `S` sides (e.g. `2d6`, `d20`).\n" +
            "• `+B` / `-P` — Add flat bonuses or subtract penalties (e.g. `1d20+7`, `2d8-2`).\n" +
            "• Combine multiple dice types and modifiers freely (e.g. `2d8+1d6+4`).",
            false)
        .AddField(
            "**Keep Highest / Lowest Dice**",
            "• `NdSh` or `NdShK` — Roll `N` dice and keep the highest `1` or `K` dice (e.g. `2d20h`, `4d6h2`).\n" +
            "• `NdSl` or `NdSlK` — Roll `N` dice and keep the lowest `1` or `K` dice (e.g. `2d20l`, `4d4l2`).",
            false)
        .AddField(
            "**Tags & Traits**",
            "• Attach a tag in `[brackets]` to a term or a `(parenthesized group)`.\n" +
            "• Single-tag rolls display the tag on the total (e.g. `Total: 35 fire`).\n" +
            "• Multi-tag rolls show both the combined total and per-tag breakdown (e.g. `Total: 15 (12 fire + 3 ice)`).",
            false)
        .AddField(
            "**Statistics Mode**",
            "• Pass the optional `Statistics` parameter on `/dice roll` to calculate **Minimum**, **Maximum**, **Average**, **Standard Deviation**, or **All** instead of rolling randomly.",
            false)
        .AddField(
            "**Examples**",
            "• `4d6h2+4` — Roll 4d6, keep the highest 2, and add 4.\n" +
            "• `2d8h+4d4l2` — Roll 2d8 keeping highest 1, plus 4d4 keeping lowest 2.\n" +
            "• `(1d6)[fire]+4[poison]` — Roll 1d6 fire plus 4 poison.\n" +
            "• `(3d4)[fire tag] + (2d8+4)[water tag]` — Grouped sections with separate tags.",
            false)
        .WithFooter($"You can save up to {IDiceRollService.MaxSavedDicePerUser} dice configurations per profile.");
    }
}
