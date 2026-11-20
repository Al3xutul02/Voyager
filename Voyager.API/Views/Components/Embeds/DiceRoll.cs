using BusinessLogic.Dtos.DiceRoll;
using BusinessLogic.Enums.Types;
using BusinessLogic.Services.Abstractions;
using DSharpPlus.Entities;
using System.Globalization;
using System.Text;

namespace Voyager.API.Views.Components.Embeds;

public static partial class Embeds
{
    private const string FormulaFieldName = "**Formula**";
    private const string RollerFooterPrefix = "Roller ID: ";

    /// <summary>
    /// Builds the dice roll result or statistics embed shown by <c>/dice roll</c>.
    /// Displays the formula, section-by-section breakdown (including individual kept/dropped
    /// dice and tags), the tag-aware total or requested statistics, and the roller's ID in
    /// the footer so interaction handlers can verify clear permissions.
    /// </summary>
    /// <param name="enumService">Maps the chosen palette color to a <see cref="DiscordColor"/>.</param>
    /// <param name="color">Palette color used for the embed (from the user's profile or server defaults).</param>
    /// <param name="callerId">The Discord snowflake ID of the user who rolled.</param>
    /// <param name="callerName">The Discord username of the user who rolled.</param>
    /// <param name="evaluation">The evaluated dice roll result and statistics.</param>
    /// <param name="savedRollName">Optional name of the saved dice configuration if a saved roll was used.</param>
    public static DiscordEmbedBuilder DiceRoll(
        IEnumService enumService,
        Color color,
        ulong callerId,
        string callerName,
        DiceEvaluationResultDto evaluation,
        string? savedRollName = null)
    {
        string title = string.IsNullOrWhiteSpace(savedRollName)
            ? $"{callerName}'s Dice Roll"
            : $"{callerName}'s Roll — {savedRollName}";

        var builder = new DiscordEmbedBuilder
        {
            Title = title,
            Color = enumService.ConvertColor(color)
        }
        .WithFooter($"{RollerFooterPrefix}{callerId}")
        .AddField(FormulaFieldName, $"`{evaluation.Formula}`", true);

        if (evaluation.StatisticsType is null)
        {
            string totalFormatted = evaluation.FormatWithTags(
                evaluation.Total.ToString(CultureInfo.InvariantCulture),
                t => t.Total,
                "0");

            builder.AddField("**Result**", $"**Total: {totalFormatted}**", true);
            builder.AddField("**Breakdown**", BuildRandomBreakdown(evaluation), false);
        }
        else
        {
            builder.AddField("**Mode**", $"Statistics ({FormatStatisticsMode(evaluation.StatisticsType.Value)})", true);
            builder.AddField("**Statistics**", BuildStatisticsSummary(evaluation, evaluation.StatisticsType.Value), false);

            if (evaluation.Sections.Count > 1)
            {
                builder.AddField("**Sections**", BuildStatisticsSectionsBreakdown(evaluation, evaluation.StatisticsType.Value), false);
            }
        }

        return builder;
    }

    /// <summary>
    /// Extracts the normalized formula string from a <see cref="DiceRoll"/> embed on a message.
    /// </summary>
    /// <param name="embed">The <see cref="DiscordEmbed"/> to inspect.</param>
    /// <returns>The formula string without surrounding backticks, or <c>null</c> if not found.</returns>
    public static string? ExtractFormulaFromDiceRollEmbed(DiscordEmbed? embed)
    {
        var field = embed?.Fields?.FirstOrDefault(f => f.Name == FormulaFieldName);
        if (field is null || string.IsNullOrWhiteSpace(field.Value))
        {
            return null;
        }

        return field.Value.Trim().Trim('`').Trim();
    }

    /// <summary>
    /// Extracts the roller's Discord snowflake ID from the footer of a <see cref="DiceRoll"/> embed.
    /// </summary>
    /// <param name="embed">The <see cref="DiscordEmbed"/> to inspect.</param>
    /// <param name="rollerId">Receives the parsed roller ID when present.</param>
    /// <returns><c>true</c> if a valid roller ID was extracted; otherwise <c>false</c>.</returns>
    public static bool TryExtractRollerIdFromDiceRollEmbed(DiscordEmbed? embed, out ulong rollerId)
    {
        rollerId = 0;
        string? footerText = embed?.Footer?.Text;
        if (string.IsNullOrWhiteSpace(footerText) || !footerText.StartsWith(RollerFooterPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        string idSpan = footerText[RollerFooterPrefix.Length..].Trim();
        return ulong.TryParse(idSpan, NumberStyles.None, CultureInfo.InvariantCulture, out rollerId);
    }

    private static string BuildRandomBreakdown(DiceEvaluationResultDto evaluation)
    {
        var sb = new StringBuilder();

        foreach (var section in evaluation.Sections)
        {
            var termParts = new List<string>(section.Terms.Count);

            foreach (var term in section.Terms)
            {
                if (!term.IsDice)
                {
                    termParts.Add(term.Expression);
                }
                else
                {
                    string rollsText = string.Join(", ", term.Rolls.Select(r => r.IsKept ? $"**{r.Value}**" : $"~~{r.Value}~~"));
                    termParts.Add($"{term.Expression} ({rollsText})");
                }
            }

            string sectionSummary = !string.IsNullOrWhiteSpace(section.Tag)
                ? $"**{section.Total} {section.Tag}**"
                : $"**{section.Total}**";

            sb.AppendLine($"• `{section.Expression}`: {string.Join(" ", termParts)} = {sectionSummary}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string BuildStatisticsSummary(DiceEvaluationResultDto evaluation, DiceStatisticsType mode)
    {
        string FormatMin() => $"**Minimum:** {evaluation.FormatWithTags(evaluation.Minimum.ToString(CultureInfo.InvariantCulture), t => t.Minimum, "0")}";
        string FormatMax() => $"**Maximum:** {evaluation.FormatWithTags(evaluation.Maximum.ToString(CultureInfo.InvariantCulture), t => t.Maximum, "0")}";
        string FormatAvg() => $"**Average:** {evaluation.FormatWithTags(evaluation.Average.ToString("0.##", CultureInfo.InvariantCulture), t => t.Average, "0.##")}";
        string FormatStd() => $"**Standard Deviation:** {evaluation.FormatWithTags(evaluation.StandardDeviation.ToString("0.##", CultureInfo.InvariantCulture), t => t.StandardDeviation, "0.##")}";

        return mode switch
        {
            DiceStatisticsType.Minimum => FormatMin(),
            DiceStatisticsType.Maximum => FormatMax(),
            DiceStatisticsType.Average => FormatAvg(),
            DiceStatisticsType.StandardDeviation => FormatStd(),
            DiceStatisticsType.All => string.Join("\n", FormatMin(), FormatMax(), FormatAvg(), FormatStd()),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported dice statistics mode.")
        };
    }

    private static string BuildStatisticsSectionsBreakdown(DiceEvaluationResultDto evaluation, DiceStatisticsType mode)
    {
        var sb = new StringBuilder();
        foreach (var section in evaluation.Sections)
        {
            string tagSuffix = !string.IsNullOrWhiteSpace(section.Tag) ? $" ({section.Tag})" : string.Empty;
            string stats = mode switch
            {
                DiceStatisticsType.Minimum => $"Min: {section.Minimum}",
                DiceStatisticsType.Maximum => $"Max: {section.Maximum}",
                DiceStatisticsType.Average => $"Avg: {section.Average.ToString("0.##", CultureInfo.InvariantCulture)}",
                DiceStatisticsType.StandardDeviation => $"StdDev: {section.StandardDeviation.ToString("0.##", CultureInfo.InvariantCulture)}",
                DiceStatisticsType.All =>
                    $"Min: {section.Minimum} | Max: {section.Maximum} | Avg: {section.Average.ToString("0.##", CultureInfo.InvariantCulture)} | StdDev: {section.StandardDeviation.ToString("0.##", CultureInfo.InvariantCulture)}",
                _ => string.Empty
            };
            sb.AppendLine($"• `{section.Expression}`{tagSuffix}: {stats}");
        }
        return sb.ToString().TrimEnd();
    }

    private static string FormatStatisticsMode(DiceStatisticsType mode) => mode switch
    {
        DiceStatisticsType.Minimum => "Minimum",
        DiceStatisticsType.Maximum => "Maximum",
        DiceStatisticsType.Average => "Average",
        DiceStatisticsType.StandardDeviation => "Standard Deviation",
        DiceStatisticsType.All => "All",
        _ => mode.ToString()
    };
}
