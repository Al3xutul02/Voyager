using BusinessLogic.Enums.Types;

namespace BusinessLogic.Dtos.DiceRoll;

/// <summary>
/// Represents a single rolled die and whether it was kept or dropped by a
/// keep-highest (<c>h</c>) or keep-lowest (<c>l</c>) modifier.
/// </summary>
/// <param name="Value">The face value rolled on the die (<c>1..Sides</c>).</param>
/// <param name="IsKept">Whether this die was kept in the sum.</param>
public record RolledDieDto(int Value, bool IsKept);

/// <summary>
/// Represents the evaluation result of a single term (either a dice term like
/// <c>4d6h2</c> or a constant modifier like <c>+4</c>) within a roll section.
/// </summary>
/// <param name="Expression">The normalized text of this term (e.g. <c>+4d6h2</c> or <c>+4</c>).</param>
/// <param name="IsDice">Whether this term is a dice roll (<c>true</c>) or a flat integer modifier (<c>false</c>).</param>
/// <param name="Sign">The sign applied to this term (<c>+1</c> or <c>-1</c>).</param>
/// <param name="Rolls">The individual dice rolled for this term (empty for flat modifiers or pure statistics).</param>
/// <param name="Total">The random roll subtotal contributed by this term (signed).</param>
/// <param name="Minimum">The minimum possible signed value of this term.</param>
/// <param name="Maximum">The maximum possible signed value of this term.</param>
/// <param name="Average">The expected signed value (mean) of this term.</param>
/// <param name="Variance">The theoretical variance of this term.</param>
public record DiceTermResultDto(
    string Expression,
    bool IsDice,
    int Sign,
    IReadOnlyList<RolledDieDto> Rolls,
    int Total,
    int Minimum,
    int Maximum,
    double Average,
    double Variance);

/// <summary>
/// Represents a section of a dice roll expression (e.g. <c>(3d4)[fire tag]</c>
/// or an untagged group of terms) with its own tag, term breakdown, and subtotals.
/// </summary>
/// <param name="Expression">The normalized expression of the section.</param>
/// <param name="Tag">Optional tag/trait attached to the section (e.g. <c>fire</c>), or <c>null</c> if untagged.</param>
/// <param name="Terms">The evaluated terms inside this section.</param>
/// <param name="Total">The random roll subtotal for this section.</param>
/// <param name="Minimum">The minimum possible subtotal for this section.</param>
/// <param name="Maximum">The maximum possible subtotal for this section.</param>
/// <param name="Average">The expected value (mean) for this section.</param>
/// <param name="Variance">The theoretical variance for this section.</param>
/// <param name="StandardDeviation">The theoretical standard deviation for this section.</param>
public record DiceSectionResultDto(
    string Expression,
    string? Tag,
    IReadOnlyList<DiceTermResultDto> Terms,
    int Total,
    int Minimum,
    int Maximum,
    double Average,
    double Variance,
    double StandardDeviation);

/// <summary>
/// Aggregated results across all sections that share the same tag/trait.
/// </summary>
/// <param name="Tag">The tag/trait name, or <c>null</c> for untagged sections.</param>
/// <param name="Total">Combined random roll subtotal for this tag.</param>
/// <param name="Minimum">Combined minimum possible subtotal for this tag.</param>
/// <param name="Maximum">Combined maximum possible subtotal for this tag.</param>
/// <param name="Average">Combined expected value (mean) for this tag.</param>
/// <param name="StandardDeviation">Combined standard deviation for this tag.</param>
public record DiceTagSummaryDto(
    string? Tag,
    int Total,
    int Minimum,
    int Maximum,
    double Average,
    double StandardDeviation);

/// <summary>
/// Complete evaluation result produced by the dice roll engine for a formula.
/// </summary>
/// <param name="Formula">The normalized dice formula string.</param>
/// <param name="StatisticsType">The requested statistics calculation mode, or <c>null</c> for a random roll.</param>
/// <param name="Sections">Ordered list of evaluated roll sections.</param>
/// <param name="TagSummaries">Aggregated totals grouped by distinct tag (in order of appearance).</param>
/// <param name="Total">Overall random roll total across all sections.</param>
/// <param name="Minimum">Overall minimum possible total across all sections.</param>
/// <param name="Maximum">Overall maximum possible total across all sections.</param>
/// <param name="Average">Overall expected value (mean) across all sections.</param>
/// <param name="StandardDeviation">Overall theoretical standard deviation across all sections.</param>
public record DiceEvaluationResultDto(
    string Formula,
    DiceStatisticsType? StatisticsType,
    IReadOnlyList<DiceSectionResultDto> Sections,
    IReadOnlyList<DiceTagSummaryDto> TagSummaries,
    int Total,
    int Minimum,
    int Maximum,
    double Average,
    double StandardDeviation)
{
    /// <summary>
    /// Formats a numeric value (and per-tag breakdown) using the tag rules:
    /// <list type="bullet">
    /// <item>No tags: <c>15</c></item>
    /// <item>Single tag (and no untagged sections): <c>35 fire</c></item>
    /// <item>Multiple tags (or mixed tagged and untagged): <c>15 (12 fire + 3 ice)</c></item>
    /// </list>
    /// </summary>
    /// <param name="overallValue">Formatted overall numeric value.</param>
    /// <param name="tagValueSelector">Selects the numeric value for each <see cref="DiceTagSummaryDto"/>.</param>
    /// <param name="numberFormat">Format string for per-tag numbers (e.g. <c>"0.##"</c>).</param>
    public string FormatWithTags(
        string overallValue,
        Func<DiceTagSummaryDto, double> tagValueSelector,
        string numberFormat = "0.##")
    {
        bool anyTagged = TagSummaries.Any(t => !string.IsNullOrWhiteSpace(t.Tag));
        if (!anyTagged)
        {
            return overallValue;
        }

        if (TagSummaries.Count == 1 && !string.IsNullOrWhiteSpace(TagSummaries[0].Tag))
        {
            return $"{overallValue} {TagSummaries[0].Tag}";
        }

        var parts = new List<string>(TagSummaries.Count);
        for (int i = 0; i < TagSummaries.Count; i++)
        {
            var summary = TagSummaries[i];
            double val = tagValueSelector(summary);
            string absFormatted = Math.Abs(val).ToString(numberFormat, System.Globalization.CultureInfo.InvariantCulture);
            string tagSuffix = !string.IsNullOrWhiteSpace(summary.Tag) ? $" {summary.Tag}" : string.Empty;

            if (i == 0)
            {
                string signPrefix = val < 0 ? "-" : string.Empty;
                parts.Add($"{signPrefix}{absFormatted}{tagSuffix}");
            }
            else
            {
                string op = val < 0 ? " - " : " + ";
                parts.Add($"{op}{absFormatted}{tagSuffix}");
            }
        }

        return $"{overallValue} ({string.Concat(parts)})";
    }
}
