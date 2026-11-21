using AutoMapper;
using BusinessLogic.Dtos.DiceRoll;
using BusinessLogic.Enums.Types;
using BusinessLogic.Services.Abstractions;
using BusinessLogic.Services.Generic;
using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories.Abstractions;
using System.Globalization;
using System.Text;

namespace BusinessLogic.Services;

/// <summary>
/// Business logic service for persisting saved user dice configurations and
/// parsing/evaluating dice roll formulas and statistics.
/// </summary>
/// <param name="mapper">The AutoMapper instance used for entity-DTO mapping.</param>
/// <param name="diceRollRepository">The repository used to query and persist saved dice roll entities.</param>
public class DiceRollService(IMapper mapper, IDiceRollRepository diceRollRepository)
    : BaseService<ulong, DiceRoll, DiceRollReadDto, DiceRollCreateDto, DiceRollUpdateDto>(mapper, diceRollRepository), IDiceRollService
{
    /// <summary>Maximum number of dice allowed in a single dice term.</summary>
    public const int MaxDicePerTerm = 100;

    /// <summary>Maximum number of faces/sides allowed on a single die.</summary>
    public const int MaxSidesPerDie = 1000;

    /// <summary>Maximum flat integer modifier allowed in a single term.</summary>
    public const int MaxFlatBonus = 100000;

    private IDiceRollRepository DiceRollRepository => (IDiceRollRepository)_repository;

    /// <inheritdoc />
    public async Task<IEnumerable<DiceRollReadDto>> GetByUserIdAsync(ulong userId)
    {
        IEnumerable<DiceRoll> diceRolls = await DiceRollRepository.GetByUserIdAsync(userId, IncludeBehavior.NoInclude);
        return _mapper.Map<IEnumerable<DiceRollReadDto>>(diceRolls);
    }

    /// <inheritdoc />
    public async Task<DiceRollReadDto?> GetByUserAndNameAsync(ulong userId, string name)
    {
        DiceRoll? diceRoll = await DiceRollRepository.GetByUserAndNameAsync(userId, name, IncludeBehavior.NoInclude);
        return _mapper.Map<DiceRollReadDto>(diceRoll);
    }

    /// <inheritdoc />
    public async Task<DiceRollReadDto?> GetByUserAndFormulaAsync(ulong userId, string formula)
    {
        string lookupFormula = TryValidateFormula(formula, out string normalized, out _)
            ? normalized
            : formula.Trim();

        DiceRoll? diceRoll = await DiceRollRepository.GetByUserAndFormulaAsync(userId, lookupFormula, IncludeBehavior.NoInclude);
        return _mapper.Map<DiceRollReadDto>(diceRoll);
    }

    /// <inheritdoc />
    public bool TryValidateFormula(string? formula, out string normalizedFormula, out string errorMessage)
    {
        normalizedFormula = string.Empty;
        errorMessage = string.Empty;

        if (!TryParseFormula(formula, out var sections, out errorMessage))
        {
            return false;
        }

        normalizedFormula = BuildNormalizedFormula(sections);
        return true;
    }

    /// <inheritdoc />
    public DiceEvaluationResultDto Evaluate(
        string formula,
        DiceStatisticsType? statisticsType = null,
        Random? random = null)
    {
        if (!TryParseFormula(formula, out var parsedSections, out string errorMessage))
        {
            throw new ArgumentException(errorMessage, nameof(formula));
        }

        random ??= Random.Shared;
        string normalizedFormula = BuildNormalizedFormula(parsedSections);

        var evaluatedSections = new List<DiceSectionResultDto>(parsedSections.Count);
        foreach (var section in parsedSections)
        {
            var evaluatedTerms = new List<DiceTermResultDto>(section.Terms.Count);
            for (int i = 0; i < section.Terms.Count; i++)
            {
                var term = section.Terms[i];
                evaluatedTerms.Add(EvaluateTerm(term, isFirstInSection: i == 0, random));
            }

            int secTotal = evaluatedTerms.Sum(t => t.Total);
            int secMin = evaluatedTerms.Sum(t => t.Minimum);
            int secMax = evaluatedTerms.Sum(t => t.Maximum);
            double secAvg = evaluatedTerms.Sum(t => t.Average);
            double secVar = evaluatedTerms.Sum(t => t.Variance);
            double secStdDev = Math.Sqrt(Math.Max(0.0, secVar));

            evaluatedSections.Add(new DiceSectionResultDto(
                Expression: BuildSectionExpression(section, isFirstSection: evaluatedSections.Count == 0),
                Tag: section.Tag,
                Terms: evaluatedTerms,
                Total: secTotal,
                Minimum: secMin,
                Maximum: secMax,
                Average: secAvg,
                Variance: secVar,
                StandardDeviation: secStdDev));
        }

        // Aggregate by tag in order of first appearance (case-insensitive for named tags, null for untagged)
        var tagOrder = new List<string?>();
        var tagGroups = new Dictionary<string, List<DiceSectionResultDto>>(StringComparer.OrdinalIgnoreCase);
        var untaggedGroup = new List<DiceSectionResultDto>();
        bool hasUntagged = false;

        foreach (var sec in evaluatedSections)
        {
            if (string.IsNullOrWhiteSpace(sec.Tag))
            {
                if (!hasUntagged)
                {
                    hasUntagged = true;
                    tagOrder.Add(null);
                }
                untaggedGroup.Add(sec);
            }
            else
            {
                if (!tagGroups.TryGetValue(sec.Tag, out var list))
                {
                    list = [];
                    tagGroups[sec.Tag] = list;
                    tagOrder.Add(sec.Tag);
                }
                list.Add(sec);
            }
        }

        var tagSummaries = new List<DiceTagSummaryDto>(tagOrder.Count);
        foreach (var tagKey in tagOrder)
        {
            var group = tagKey is null ? untaggedGroup : tagGroups[tagKey];
            double groupVar = group.Sum(s => s.Variance);
            tagSummaries.Add(new DiceTagSummaryDto(
                Tag: tagKey,
                Total: group.Sum(s => s.Total),
                Minimum: group.Sum(s => s.Minimum),
                Maximum: group.Sum(s => s.Maximum),
                Average: group.Sum(s => s.Average),
                StandardDeviation: Math.Sqrt(Math.Max(0.0, groupVar))));
        }

        int overallTotal = evaluatedSections.Sum(s => s.Total);
        int overallMin = evaluatedSections.Sum(s => s.Minimum);
        int overallMax = evaluatedSections.Sum(s => s.Maximum);
        double overallAvg = evaluatedSections.Sum(s => s.Average);
        double overallVar = evaluatedSections.Sum(s => s.Variance);
        double overallStdDev = Math.Sqrt(Math.Max(0.0, overallVar));

        return new DiceEvaluationResultDto(
            Formula: normalizedFormula,
            StatisticsType: statisticsType,
            Sections: evaluatedSections,
            TagSummaries: tagSummaries,
            Total: overallTotal,
            Minimum: overallMin,
            Maximum: overallMax,
            Average: overallAvg,
            StandardDeviation: overallStdDev);
    }

    // ---- Internal AST Models --------------------------------------------

    private enum KeepMode
    {
        All,
        Highest,
        Lowest
    }

    private sealed record ParsedTerm(
        int Sign,
        bool IsDice,
        int Count,
        int Sides,
        KeepMode KeepMode,
        int KeepCount,
        int ConstantValue);

    private sealed record ParsedSection(
        int OuterSign,
        bool WasParenthesized,
        IReadOnlyList<ParsedTerm> Terms,
        string? Tag);

    // ---- Evaluation & Exact Order-Statistic DP --------------------------

    private static DiceTermResultDto EvaluateTerm(ParsedTerm term, bool isFirstInSection, Random random)
    {
        string expr = FormatTerm(term, includeLeadingPlus: !isFirstInSection);

        if (!term.IsDice)
        {
            int signedConst = term.Sign * term.ConstantValue;
            return new DiceTermResultDto(
                Expression: expr,
                IsDice: false,
                Sign: term.Sign,
                Rolls: [],
                Total: signedConst,
                Minimum: signedConst,
                Maximum: signedConst,
                Average: signedConst,
                Variance: 0.0);
        }

        // 1. Roll N dice randomly
        var rawRolls = new int[term.Count];
        for (int i = 0; i < term.Count; i++)
        {
            rawRolls[i] = random.Next(1, term.Sides + 1);
        }

        // Determine which indices are kept
        var keptFlags = new bool[term.Count];
        if (term.KeepMode == KeepMode.All || term.KeepCount == term.Count)
        {
            Array.Fill(keptFlags, true);
        }
        else
        {
            var indices = Enumerable.Range(0, term.Count).ToArray();
            if (term.KeepMode == KeepMode.Highest)
            {
                Array.Sort(indices, (a, b) => rawRolls[b].CompareTo(rawRolls[a]));
            }
            else
            {
                Array.Sort(indices, (a, b) => rawRolls[a].CompareTo(rawRolls[b]));
            }

            for (int i = 0; i < term.KeepCount; i++)
            {
                keptFlags[indices[i]] = true;
            }
        }

        var rolledDice = new List<RolledDieDto>(term.Count);
        int unsignedRollSum = 0;
        for (int i = 0; i < term.Count; i++)
        {
            rolledDice.Add(new RolledDieDto(rawRolls[i], keptFlags[i]));
            if (keptFlags[i])
            {
                unsignedRollSum += rawRolls[i];
            }
        }

        // 2. Compute exact Minimum, Maximum, Average, and Variance
        int unsignedMin = term.KeepCount;
        int unsignedMax = term.KeepCount * term.Sides;
        var (unsignedMean, unsignedVariance) = ComputeExactDiceMeanAndVariance(
            term.Count,
            term.Sides,
            term.KeepMode,
            term.KeepCount);

        int signedTotal = term.Sign * unsignedRollSum;
        int signedMin = term.Sign > 0 ? unsignedMin : -unsignedMax;
        int signedMax = term.Sign > 0 ? unsignedMax : -unsignedMin;
        double signedAvg = term.Sign * unsignedMean;

        return new DiceTermResultDto(
            Expression: expr,
            IsDice: true,
            Sign: term.Sign,
            Rolls: rolledDice,
            Total: signedTotal,
            Minimum: signedMin,
            Maximum: signedMax,
            Average: signedAvg,
            Variance: unsignedVariance);
    }

    /// <summary>
    /// Computes the exact theoretical mean and variance of the sum of <paramref name="keepCount"/>
    /// dice kept from <paramref name="count"/> rolls of a <paramref name="sides"/>-sided die.
    /// Uses closed-form formulas when all dice are kept, and exact order-statistic dynamic
    /// programming when <c>h</c> (keep highest) or <c>l</c> (keep lowest) is active.
    /// </summary>
    private static (double Mean, double Variance) ComputeExactDiceMeanAndVariance(
        int count,
        int sides,
        KeepMode keepMode,
        int keepCount)
    {
        if (sides == 1)
        {
            return (keepCount, 0.0);
        }

        if (keepMode == KeepMode.All || keepCount == count)
        {
            double singleMean = (sides + 1) / 2.0;
            double singleVar = ((double)sides * sides - 1.0) / 12.0;
            return (count * singleMean, count * singleVar);
        }

        // Note: By symmetry of a uniform die 1..S, if X is the sum of the K lowest of N dice,
        // then (K * (S + 1) - X) has the exact same distribution as the sum of the K highest of N dice.
        // Thus Var(Highest K) == Var(Lowest K) and E[Lowest K] == K * (S + 1) - E[Highest K].
        // We compute the exact distribution of the sum of the K highest dice by iterating face values
        // from S down to 1.
        //
        // At face value v (from S down to 1), given m dice remain to be placed among values 1..v,
        // the number of dice c that land on value v follows Binomial(m, 1 / v).
        // Those c dice contribute min(c, kRemaining) * v to the kept sum!

        // Precompute binomial probabilities Binom(m, c, 1/v) on the fly using Pascal's triangle
        double[,] binom = new double[count + 1, count + 1];
        for (int n = 0; n <= count; n++)
        {
            binom[n, 0] = 1.0;
            for (int r = 1; r <= n; r++)
            {
                binom[n, r] = binom[n - 1, r - 1] + binom[n - 1, r];
            }
        }

        int maxSum = keepCount * sides;
        // dp[m, kRem, sum] = probability that m dice have been placed in {v..S},
        // kRem kept slots remain to be filled, and the accumulated kept sum so far is `sum`.
        // Instead of full sum array if sides is large, track (Prob, MeanSum, MeanSqSum) per (m, kRem)!
        // Because if we know P(state), E[Sum | state], E[Sum^2 | state], adding delta = addK * v updates:
        //   NewP += P
        //   NewSum1 += E[Sum + delta] = Sum1 + P * delta
        //   NewSum2 += E[(Sum + delta)^2] = Sum2 + 2 * delta * Sum1 + P * delta^2
        // This runs in O(S * N^2 * K) time and O(N * K) memory — completely independent of maxSum!
        double[,] prob = new double[count + 1, keepCount + 1];
        double[,] mom1 = new double[count + 1, keepCount + 1];
        double[,] mom2 = new double[count + 1, keepCount + 1];

        prob[count, keepCount] = 1.0;

        double[,] nextProb = new double[count + 1, keepCount + 1];
        double[,] nextMom1 = new double[count + 1, keepCount + 1];
        double[,] nextMom2 = new double[count + 1, keepCount + 1];

        for (int v = sides; v >= 1; v--)
        {
            Array.Clear(nextProb);
            Array.Clear(nextMom1);
            Array.Clear(nextMom2);

            double pFace = 1.0 / v;
            double qFace = 1.0 - pFace;

            for (int m = 0; m <= count; m++)
            {
                for (int kRem = 0; kRem <= keepCount; kRem++)
                {
                    double pState = prob[m, kRem];
                    if (pState == 0.0)
                    {
                        continue;
                    }

                    double s1 = mom1[m, kRem];
                    double s2 = mom2[m, kRem];

                    if (m == 0 || v == 1)
                    {
                        // All remaining m dice must land on v = 1 (or m == 0)
                        int c = m;
                        int addK = Math.Min(c, kRem);
                        int newKRem = kRem - addK;
                        double delta = (double)addK * v;

                        nextProb[0, newKRem] += pState;
                        nextMom1[0, newKRem] += s1 + pState * delta;
                        nextMom2[0, newKRem] += s2 + 2.0 * delta * s1 + pState * delta * delta;
                        continue;
                    }

                    for (int c = 0; c <= m; c++)
                    {
                        double transProb = binom[m, c] * Math.Pow(pFace, c) * Math.Pow(qFace, m - c);
                        int addK = Math.Min(c, kRem);
                        int newM = m - c;
                        int newKRem = kRem - addK;
                        double delta = (double)addK * v;

                        double wProb = pState * transProb;
                        double wS1 = s1 * transProb;
                        double wS2 = s2 * transProb;

                        nextProb[newM, newKRem] += wProb;
                        nextMom1[newM, newKRem] += wS1 + wProb * delta;
                        nextMom2[newM, newKRem] += wS2 + 2.0 * delta * wS1 + wProb * delta * delta;
                    }
                }
            }

            (prob, nextProb) = (nextProb, prob);
            (mom1, nextMom1) = (nextMom1, mom1);
            (mom2, nextMom2) = (nextMom2, mom2);
        }

        double highestMean = mom1[0, 0];
        double highestSecondMoment = mom2[0, 0];
        double variance = Math.Max(0.0, highestSecondMoment - highestMean * highestMean);

        double finalMean = keepMode == KeepMode.Highest
            ? highestMean
            : keepCount * (sides + 1.0) - highestMean;

        return (finalMean, variance);
    }

    // ---- Parser ---------------------------------------------------------

    private static bool TryParseFormula(
        string? input,
        out List<ParsedSection> sections,
        out string errorMessage)
    {
        sections = [];
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
        {
            errorMessage = "Error: Dice formula cannot be empty.";
            return false;
        }

        string s = input.Trim();
        int pos = 0;
        int len = s.Length;

        // Pending untagged single terms are grouped into an untagged section until a parenthesized
        // or tagged term is encountered.
        var parsedSections = new List<ParsedSection>();
        var pendingUntaggedTerms = new List<ParsedTerm>();

        void FlushPendingUntagged()
        {
            if (pendingUntaggedTerms.Count > 0)
            {
                parsedSections.Add(new ParsedSection(
                    OuterSign: 1,
                    WasParenthesized: false,
                    Terms: [.. pendingUntaggedTerms],
                    Tag: null));
                pendingUntaggedTerms.Clear();
            }
        }

        bool isFirstElement = true;

        while (pos < len)
        {
            SkipWhitespace(s, ref pos);
            if (pos >= len)
            {
                break;
            }

            int sign = 1;
            if (s[pos] == '+' || s[pos] == '-')
            {
                sign = s[pos] == '-' ? -1 : 1;
                pos++;
                SkipWhitespace(s, ref pos);
                if (pos >= len)
                {
                    errorMessage = "Error: Unexpected end of formula after '+' or '-'.";
                    return false;
                }
            }
            else if (!isFirstElement)
            {
                errorMessage = $"Error: Expected '+' or '-' at position {pos + 1}.";
                return false;
            }

            isFirstElement = false;

            if (s[pos] == '(')
            {
                pos++; // consume '('
                if (!TryParseInnerTerms(s, ref pos, stopAtCloseParen: true, out var innerTerms, out errorMessage))
                {
                    return false;
                }

                if (pos >= len || s[pos] != ')')
                {
                    errorMessage = "Error: Missing closing parenthesis ')'.";
                    return false;
                }
                pos++; // consume ')'

                SkipWhitespace(s, ref pos);
                string? tag = null;
                if (pos < len && s[pos] == '[')
                {
                    if (!TryParseTag(s, ref pos, out tag, out errorMessage))
                    {
                        return false;
                    }
                }

                // Apply outer sign to inner terms if negative
                var adjustedTerms = sign == 1
                    ? innerTerms
                    : innerTerms.Select(t => t with { Sign = t.Sign * sign }).ToList();

                FlushPendingUntagged();
                parsedSections.Add(new ParsedSection(
                    OuterSign: sign,
                    WasParenthesized: true,
                    Terms: adjustedTerms,
                    Tag: tag));
            }
            else
            {
                if (!TryParseSingleTermBody(s, ref pos, sign, out var term, out errorMessage))
                {
                    return false;
                }

                SkipWhitespace(s, ref pos);
                if (pos < len && s[pos] == '[')
                {
                    if (!TryParseTag(s, ref pos, out string? tag, out errorMessage))
                    {
                        return false;
                    }

                    FlushPendingUntagged();
                    parsedSections.Add(new ParsedSection(
                        OuterSign: 1,
                        WasParenthesized: false,
                        Terms: [term],
                        Tag: tag));
                }
                else
                {
                    pendingUntaggedTerms.Add(term);
                }
            }
        }

        FlushPendingUntagged();

        if (parsedSections.Count == 0)
        {
            errorMessage = "Error: Dice formula contains no valid terms.";
            return false;
        }

        sections = parsedSections;
        return true;
    }

    private static bool TryParseInnerTerms(
        string s,
        ref int pos,
        bool stopAtCloseParen,
        out List<ParsedTerm> terms,
        out string errorMessage)
    {
        terms = [];
        errorMessage = string.Empty;
        int len = s.Length;
        bool isFirst = true;

        while (pos < len)
        {
            SkipWhitespace(s, ref pos);
            if (pos >= len || (stopAtCloseParen && s[pos] == ')'))
            {
                break;
            }

            int sign = 1;
            if (s[pos] == '+' || s[pos] == '-')
            {
                sign = s[pos] == '-' ? -1 : 1;
                pos++;
                SkipWhitespace(s, ref pos);
                if (pos >= len || (stopAtCloseParen && s[pos] == ')'))
                {
                    errorMessage = "Error: Unexpected end of group after '+' or '-'.";
                    return false;
                }
            }
            else if (!isFirst)
            {
                errorMessage = $"Error: Expected '+' or '-' inside parentheses at position {pos + 1}.";
                return false;
            }

            isFirst = false;

            if (!TryParseSingleTermBody(s, ref pos, sign, out var term, out errorMessage))
            {
                return false;
            }

            terms.Add(term);
        }

        if (terms.Count == 0)
        {
            errorMessage = "Error: Empty parentheses '()' are not allowed.";
            return false;
        }

        return true;
    }

    private static bool TryParseSingleTermBody(
        string s,
        ref int pos,
        int sign,
        out ParsedTerm term,
        out string errorMessage)
    {
        term = null!;
        errorMessage = string.Empty;
        int len = s.Length;

        int numberStart = pos;
        while (pos < len && char.IsAsciiDigit(s[pos]))
        {
            pos++;
        }

        bool hasLeadingNumber = pos > numberStart;
        int leadingNumber = 0;
        if (hasLeadingNumber)
        {
            if (!int.TryParse(s.AsSpan(numberStart, pos - numberStart), NumberStyles.None, CultureInfo.InvariantCulture, out leadingNumber))
            {
                errorMessage = "Error: Number in dice formula is too large.";
                return false;
            }
        }

        // Check if next character is 'd' or 'D' (dice roll)
        if (pos < len && (s[pos] == 'd' || s[pos] == 'D'))
        {
            pos++; // consume 'd'
            int count = hasLeadingNumber ? leadingNumber : 1;
            if (count < 1 || count > MaxDicePerTerm)
            {
                errorMessage = $"Error: Dice count must be between 1 and {MaxDicePerTerm}.";
                return false;
            }

            int sidesStart = pos;
            while (pos < len && char.IsAsciiDigit(s[pos]))
            {
                pos++;
            }

            if (pos == sidesStart)
            {
                errorMessage = "Error: Missing die sides after 'd'.";
                return false;
            }

            if (!int.TryParse(s.AsSpan(sidesStart, pos - sidesStart), NumberStyles.None, CultureInfo.InvariantCulture, out int sides)
                || sides < 1 || sides > MaxSidesPerDie)
            {
                errorMessage = $"Error: Die sides must be between 1 and {MaxSidesPerDie}.";
                return false;
            }

            KeepMode keepMode = KeepMode.All;
            int keepCount = count;

            if (pos < len && (s[pos] == 'h' || s[pos] == 'H' || s[pos] == 'l' || s[pos] == 'L'))
            {
                char keepChar = char.ToLowerInvariant(s[pos]);
                keepMode = keepChar == 'h' ? KeepMode.Highest : KeepMode.Lowest;
                pos++;

                int keepStart = pos;
                while (pos < len && char.IsAsciiDigit(s[pos]))
                {
                    pos++;
                }

                if (pos > keepStart)
                {
                    if (!int.TryParse(s.AsSpan(keepStart, pos - keepStart), NumberStyles.None, CultureInfo.InvariantCulture, out keepCount))
                    {
                        errorMessage = "Error: Invalid keep count after 'h' or 'l'.";
                        return false;
                    }
                }
                else
                {
                    // E.g. "2d8h" -> keep highest 1 roll
                    keepCount = 1;
                }

                if (keepCount < 1 || keepCount > count)
                {
                    errorMessage = $"Error: Keep count ({keepCount}) must be between 1 and the number of dice rolled ({count}).";
                    return false;
                }
            }

            term = new ParsedTerm(
                Sign: sign,
                IsDice: true,
                Count: count,
                Sides: sides,
                KeepMode: keepMode,
                KeepCount: keepCount,
                ConstantValue: 0);
            return true;
        }

        // Otherwise, must be a flat integer constant
        if (!hasLeadingNumber)
        {
            errorMessage = $"Error: Unexpected character '{s[pos]}' at position {pos + 1}.";
            return false;
        }

        if (leadingNumber > MaxFlatBonus)
        {
            errorMessage = $"Error: Bonus/penalty cannot exceed {MaxFlatBonus}.";
            return false;
        }

        term = new ParsedTerm(
            Sign: sign,
            IsDice: false,
            Count: 0,
            Sides: 0,
            KeepMode: KeepMode.All,
            KeepCount: 0,
            ConstantValue: leadingNumber);
        return true;
    }

    private static bool TryParseTag(
        string s,
        ref int pos,
        out string? tag,
        out string errorMessage)
    {
        tag = null;
        errorMessage = string.Empty;

        // pos is at '['
        pos++; // consume '['
        int closeIdx = s.IndexOf(']', pos);
        if (closeIdx < 0)
        {
            errorMessage = "Error: Missing closing bracket ']' for tag.";
            return false;
        }

        string rawTag = s[pos..closeIdx].Trim();
        if (string.IsNullOrWhiteSpace(rawTag))
        {
            errorMessage = "Error: Tag inside '[]' cannot be empty.";
            return false;
        }

        if (rawTag.Contains('[') || rawTag.Contains(']'))
        {
            errorMessage = "Error: Nested brackets are not allowed inside a tag.";
            return false;
        }

        tag = rawTag;
        pos = closeIdx + 1;
        return true;
    }

    private static void SkipWhitespace(string s, ref int pos)
    {
        while (pos < s.Length && char.IsWhiteSpace(s[pos]))
        {
            pos++;
        }
    }

    // ---- Formatting Helpers ---------------------------------------------

    private static string BuildNormalizedFormula(IReadOnlyList<ParsedSection> sections)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < sections.Count; i++)
        {
            sb.Append(BuildSectionExpression(sections[i], isFirstSection: i == 0));
        }
        return sb.ToString();
    }

    private static string BuildSectionExpression(ParsedSection section, bool isFirstSection)
    {
        var sb = new StringBuilder();

        if (section.WasParenthesized || (section.Tag is not null && section.Terms.Count > 1))
        {
            if (section.OuterSign < 0)
            {
                sb.Append('-');
            }
            else if (!isFirstSection)
            {
                sb.Append('+');
            }

            sb.Append('(');
            for (int i = 0; i < section.Terms.Count; i++)
            {
                // Reverse the outer sign inside the display parentheses so -(2d6+4) prints cleanly
                var displayTerm = section.OuterSign < 0
                    ? section.Terms[i] with { Sign = section.Terms[i].Sign * -1 }
                    : section.Terms[i];
                sb.Append(FormatTerm(displayTerm, includeLeadingPlus: i > 0));
            }
            sb.Append(')');
            if (!string.IsNullOrWhiteSpace(section.Tag))
            {
                sb.Append('[').Append(section.Tag).Append(']');
            }
        }
        else
        {
            for (int i = 0; i < section.Terms.Count; i++)
            {
                bool includePlus = !isFirstSection || i > 0;
                sb.Append(FormatTerm(section.Terms[i], includeLeadingPlus: includePlus));
            }
            if (!string.IsNullOrWhiteSpace(section.Tag))
            {
                sb.Append('[').Append(section.Tag).Append(']');
            }
        }

        return sb.ToString();
    }

    private static string FormatTerm(ParsedTerm term, bool includeLeadingPlus)
    {
        string signPrefix = term.Sign < 0
            ? "-"
            : (includeLeadingPlus ? "+" : string.Empty);

        if (!term.IsDice)
        {
            return $"{signPrefix}{term.ConstantValue}";
        }

        string keepSuffix = term.KeepMode switch
        {
            KeepMode.Highest => $"h{term.KeepCount}",
            KeepMode.Lowest => $"l{term.KeepCount}",
            _ => string.Empty
        };

        return $"{signPrefix}{term.Count}d{term.Sides}{keepSuffix}";
    }
}
