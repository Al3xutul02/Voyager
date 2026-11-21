using AutoMapper;
using BusinessLogic.Dtos.DiceRoll;
using BusinessLogic.Enums.Types;
using BusinessLogic.Services;
using NSubstitute;
using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories.Abstractions;
using VoyagerTests.TestHelpers;

namespace VoyagerTests.Services;

/// <summary>
/// Tests for <see cref="DiceRollService"/> covering CRUD persistence, formula
/// validation, parsing of complex expressions (bonuses, penalties, multiple dice,
/// keep highest/lowest, parenthesized and inline tags), random roll evaluation,
/// exact statistics (Minimum, Maximum, Average, Standard Deviation, All), and
/// tag-aware total formatting.
/// </summary>
[TestClass]
public class DiceRollServiceTests
{
    private readonly IDiceRollRepository _repository = Substitute.For<IDiceRollRepository>();
    private readonly IMapper _mapper = MapperHelper.CreateMapper();
    private DiceRollService _sut = null!;

    /// <summary>
    /// Deterministic <see cref="Random"/> fake that returns a scripted sequence
    /// of face values in <see cref="Next(int, int)"/>.
    /// </summary>
    private sealed class SequenceRandom(params int[] values) : Random
    {
        private readonly int[] _values = values;
        private int _index;

        public override int Next(int minValue, int maxValue)
        {
            int val = _values[_index % _values.Length];
            _index++;
            return Math.Clamp(val, minValue, maxValue - 1);
        }
    }

    private static DiceRoll MakeDiceRoll(ulong id = 1, ulong userId = 10, string name = "Fireball", string formula = "6d6[fire]") =>
        new()
        {
            Id = id,
            UserId = userId,
            Name = name,
            Formula = formula
        };

    [TestInitialize]
    public void Setup() => _sut = new DiceRollService(_mapper, _repository);

    // ---- Repository / CRUD Delegation -----------------------------------

    [TestMethod]
    public async Task GetByUserIdAsync_ReturnsMappedDtos()
    {
        var rolls = new List<DiceRoll>
        {
            MakeDiceRoll(1, 10, "A", "1d20"),
            MakeDiceRoll(2, 10, "B", "2d6+3")
        };
        _repository.GetByUserIdAsync(10, Arg.Any<IncludeBehavior>(), Arg.Any<Func<IQueryable<DiceRoll>, IQueryable<DiceRoll>>?>())
                   .Returns(rolls);

        var result = (await _sut.GetByUserIdAsync(10)).ToList();

        Assert.HasCount(2, result);
        Assert.AreEqual("A", result[0].Name);
        Assert.AreEqual("2d6+3", result[1].Formula);
    }

    [TestMethod]
    public async Task GetByUserAndNameAsync_WhenFound_ReturnsMappedDto()
    {
        _repository.GetByUserAndNameAsync(10, "Fireball", Arg.Any<IncludeBehavior>(), Arg.Any<Func<IQueryable<DiceRoll>, IQueryable<DiceRoll>>?>())
                   .Returns(MakeDiceRoll(5, 10, "Fireball", "8d6[fire]"));

        var result = await _sut.GetByUserAndNameAsync(10, "Fireball");

        Assert.IsNotNull(result);
        Assert.AreEqual(5UL, result!.Id);
        Assert.AreEqual("8d6[fire]", result.Formula);
    }

    [TestMethod]
    public async Task GetByUserAndFormulaAsync_NormalizesFormulaBeforeLookup()
    {
        _repository.GetByUserAndFormulaAsync(10, "4d6h2+4", Arg.Any<IncludeBehavior>(), Arg.Any<Func<IQueryable<DiceRoll>, IQueryable<DiceRoll>>?>())
                   .Returns(MakeDiceRoll(7, 10, "SavedStat", "4d6h2+4"));

        var result = await _sut.GetByUserAndFormulaAsync(10, " 4D6H2 + 4 ");

        Assert.IsNotNull(result);
        Assert.AreEqual("SavedStat", result!.Name);
    }

    [TestMethod]
    public async Task CreateAsync_MapsAndSavesDiceRoll()
    {
        var dto = new DiceRollCreateDto(10, "Greataxe", "1d12+4");

        await _sut.CreateAsync(dto);

        await _repository.Received(1).AddAsync(Arg.Is<DiceRoll>(d =>
            d.UserId == 10 && d.Name == "Greataxe" && d.Formula == "1d12+4"));
        await _repository.Received(1).SaveAsync();
    }

    // ---- Formula Validation ---------------------------------------------

    [TestMethod]
    public void TryValidateFormula_ValidExamplesFromSpec_ReturnsTrueAndNormalizes()
    {
        Assert.IsTrue(_sut.TryValidateFormula("4d6h2+4", out string norm1, out _));
        Assert.AreEqual("4d6h2+4", norm1);

        Assert.IsTrue(_sut.TryValidateFormula("2d8h+4d4l2", out string norm2, out _));
        Assert.AreEqual("2d8h1+4d4l2", norm2);

        Assert.IsTrue(_sut.TryValidateFormula("(1d6)[fire]+4[poison]", out string norm3, out _));
        Assert.AreEqual("(1d6)[fire]+4[poison]", norm3);

        Assert.IsTrue(_sut.TryValidateFormula("(3d4)[fire tag] + (2d8+4)[water tag]", out string norm4, out _));
        Assert.AreEqual("(3d4)[fire tag]+(2d8+4)[water tag]", norm4);
    }

    [TestMethod]
    public void TryValidateFormula_InvalidFormulas_ReturnsFalseWithDescriptiveError()
    {
        Assert.IsFalse(_sut.TryValidateFormula("", out _, out string errEmpty));
        StringAssert.Contains(errEmpty, "empty");

        Assert.IsFalse(_sut.TryValidateFormula("4d6h5", out _, out string errKeepTooHigh));
        StringAssert.Contains(errKeepTooHigh, "Keep count");

        Assert.IsFalse(_sut.TryValidateFormula("2d0", out _, out string errZeroSides));
        StringAssert.Contains(errZeroSides, "sides");

        Assert.IsFalse(_sut.TryValidateFormula("0d6", out _, out string errZeroDice));
        StringAssert.Contains(errZeroDice, "count");

        Assert.IsFalse(_sut.TryValidateFormula("(2d6+4", out _, out string errParen));
        StringAssert.Contains(errParen, "parenthesis");

        Assert.IsFalse(_sut.TryValidateFormula("2d6[fire", out _, out string errBracket));
        StringAssert.Contains(errBracket, "bracket");

        Assert.IsFalse(_sut.TryValidateFormula("2d6[]", out _, out string errEmptyTag));
        StringAssert.Contains(errEmptyTag, "Tag");

        Assert.IsFalse(_sut.TryValidateFormula("2d6+", out _, out string errTrailingOp));
        StringAssert.Contains(errTrailingOp, "Unexpected end");
    }

    // ---- Evaluate: Spec Example 1 (4d6h2+4) -----------------------------

    [TestMethod]
    public void Evaluate_4d6h2Plus4_KeepsHighest2AndAdds4()
    {
        // Rolls: 2, 6, 1, 5 -> highest 2 are 6 and 5 (sum = 11), + 4 = 15
        var rng = new SequenceRandom(2, 6, 1, 5);

        var result = _sut.Evaluate("4d6h2+4", random: rng);

        Assert.AreEqual(15, result.Total);
        // Min: 2 * 1 + 4 = 6; Max: 2 * 6 + 4 = 16
        Assert.AreEqual(6, result.Minimum);
        Assert.AreEqual(16, result.Maximum);

        var diceTerm = result.Sections[0].Terms[0];
        Assert.HasCount(4, diceTerm.Rolls);
        Assert.AreEqual(2, diceTerm.Rolls.Count(r => r.IsKept));
        CollectionAssert.AreEquivalent(
            new[] { 6, 5 },
            diceTerm.Rolls.Where(r => r.IsKept).Select(r => r.Value).ToList());
    }

    // ---- Evaluate: Spec Example 2 (2d8h+4d4l2) --------------------------

    [TestMethod]
    public void Evaluate_2d8hPlus4d4l2_KeepsHighest1Of2d8AndLowest2Of4d4()
    {
        // 2d8 rolls: 3, 7 -> keep highest 1 => 7
        // 4d4 rolls: 4, 2, 1, 3 -> keep lowest 2 => 1 + 2 = 3
        // Total = 7 + 3 = 10
        var rng = new SequenceRandom(3, 7, 4, 2, 1, 3);

        var result = _sut.Evaluate("2d8h+4d4l2", random: rng);

        Assert.AreEqual(10, result.Total);
        // Min: 1 (from 2d8h1) + 2 (from 4d4l2) = 3
        // Max: 8 (from 2d8h1) + 8 (from 4d4l2) = 16
        Assert.AreEqual(3, result.Minimum);
        Assert.AreEqual(16, result.Maximum);
    }

    // ---- Evaluate: Spec Example 3 ((1d6)[fire]+4[poison]) ---------------

    [TestMethod]
    public void Evaluate_1d6FirePlus4Poison_TracksTagsSeparatelyAndFormatsTotal()
    {
        var rng = new SequenceRandom(5);

        var result = _sut.Evaluate("(1d6)[fire]+4[poison]", random: rng);

        Assert.AreEqual(9, result.Total);
        Assert.HasCount(2, result.Sections);
        Assert.AreEqual("fire", result.Sections[0].Tag);
        Assert.AreEqual(5, result.Sections[0].Total);
        Assert.AreEqual("poison", result.Sections[1].Tag);
        Assert.AreEqual(4, result.Sections[1].Total);

        string formatted = result.FormatWithTags(result.Total.ToString(), t => t.Total, "0");
        Assert.AreEqual("9 (5 fire + 4 poison)", formatted);
    }

    // ---- Evaluate: Spec Example 4 ((3d4)[fire tag] + (2d8+4)[water tag]) -

    [TestMethod]
    public void Evaluate_GroupedTaggedSections_ComputesEachSectionAndCombinedTotal()
    {
        // 3d4: 2, 3, 4 => 9 [fire tag]
        // 2d8+4: 5, 6 (+4) => 15 [water tag]
        // Total = 24
        var rng = new SequenceRandom(2, 3, 4, 5, 6);

        var result = _sut.Evaluate("(3d4)[fire tag] + (2d8+4)[water tag]", random: rng);

        Assert.AreEqual(24, result.Total);
        Assert.HasCount(2, result.Sections);
        Assert.AreEqual(9, result.Sections[0].Total);
        Assert.AreEqual("fire tag", result.Sections[0].Tag);
        Assert.AreEqual(15, result.Sections[1].Total);
        Assert.AreEqual("water tag", result.Sections[1].Tag);

        string formatted = result.FormatWithTags(result.Total.ToString(), t => t.Total, "0");
        Assert.AreEqual("24 (9 fire tag + 15 water tag)", formatted);
    }

    // ---- Tag Formatting Rules -------------------------------------------

    [TestMethod]
    public void FormatWithTags_SingleTag_ShowsTotalWithSingleTagName()
    {
        // Example from user specification: "Total: 35 fire"
        var rng = new SequenceRandom(6, 6, 6, 6, 6, 5);
        var result = _sut.Evaluate("(6d6)[fire]", random: rng);

        Assert.AreEqual(35, result.Total);
        string formatted = result.FormatWithTags(result.Total.ToString(), t => t.Total, "0");
        Assert.AreEqual("35 fire", formatted);
    }

    [TestMethod]
    public void FormatWithTags_TwoTags_ShowsTotalWithParenthesizedTagBreakdown()
    {
        // Example from user specification: "Total: 15 (12 fire + 3 ice)"
        var rng = new SequenceRandom(6, 6, 3);
        var result = _sut.Evaluate("(2d6)[fire] + (1d4)[ice]", random: rng);

        Assert.AreEqual(15, result.Total);
        string formatted = result.FormatWithTags(result.Total.ToString(), t => t.Total, "0");
        Assert.AreEqual("15 (12 fire + 3 ice)", formatted);
    }

    [TestMethod]
    public void FormatWithTags_NoTags_ShowsPlainNumber()
    {
        var rng = new SequenceRandom(4, 5);
        var result = _sut.Evaluate("2d6+3", random: rng);

        Assert.AreEqual(12, result.Total);
        string formatted = result.FormatWithTags(result.Total.ToString(), t => t.Total, "0");
        Assert.AreEqual("12", formatted);
    }

    // ---- Exact Statistics Verification ----------------------------------

    [TestMethod]
    public void Evaluate_StandardDieStatistics_MatchesClosedFormMeanAndStdDev()
    {
        // For 1d6: Min = 1, Max = 6, Mean = 3.5, Variance = (36 - 1) / 12 = 35 / 12 ≈ 2.916667, StdDev = sqrt(35/12) ≈ 1.707825
        var result = _sut.Evaluate("1d6", DiceStatisticsType.All);

        Assert.AreEqual(1, result.Minimum);
        Assert.AreEqual(6, result.Maximum);
        Assert.AreEqual(3.5, result.Average, 1e-6);
        Assert.AreEqual(Math.Sqrt(35.0 / 12.0), result.StandardDeviation, 1e-6);
    }

    [TestMethod]
    public void Evaluate_KeepHighestAndKeepLowestStatistics_MatchesBruteForceExactDistribution()
    {
        // Brute-force 2d4h1 (32 outcomes? 4^2 = 16 outcomes):
        // Outcomes of max(d1, d2) for d1, d2 in 1..4:
        // 1: (1,1) -> 1 outcome
        // 2: (1,2),(2,1),(2,2) -> 3 outcomes
        // 3: (1,3),(3,1),(2,3),(3,2),(3,3) -> 5 outcomes
        // 4: 7 outcomes
        // Mean = (1*1 + 2*3 + 3*5 + 4*7) / 16 = (1 + 6 + 15 + 28) / 16 = 50 / 16 = 3.125
        // E[X^2] = (1*1 + 4*3 + 9*5 + 16*7) / 16 = (1 + 12 + 45 + 112) / 16 = 170 / 16 = 10.625
        // Var = 10.625 - 3.125^2 = 10.625 - 9.765625 = 0.859375
        var highestResult = _sut.Evaluate("2d4h1", DiceStatisticsType.All);
        Assert.AreEqual(1, highestResult.Minimum);
        Assert.AreEqual(4, highestResult.Maximum);
        Assert.AreEqual(3.125, highestResult.Average, 1e-6);
        Assert.AreEqual(Math.Sqrt(0.859375), highestResult.StandardDeviation, 1e-6);

        // For 2d4l1: by symmetry Mean = 1 * (4 + 1) - 3.125 = 1.875, Var = 0.859375
        var lowestResult = _sut.Evaluate("2d4l1", DiceStatisticsType.All);
        Assert.AreEqual(1, lowestResult.Minimum);
        Assert.AreEqual(4, lowestResult.Maximum);
        Assert.AreEqual(1.875, lowestResult.Average, 1e-6);
        Assert.AreEqual(Math.Sqrt(0.859375), lowestResult.StandardDeviation, 1e-6);
    }

    [TestMethod]
    public void Evaluate_PenaltiesAndNegativeDice_ComputesMinAndMaxCorrectly()
    {
        // 1d6 - 1d4 - 2:
        // Min = 1 - 4 - 2 = -5
        // Max = 6 - 1 - 2 = 3
        // Avg = 3.5 - 2.5 - 2 = -1.0
        var result = _sut.Evaluate("1d6-1d4-2", DiceStatisticsType.All);

        Assert.AreEqual(-5, result.Minimum);
        Assert.AreEqual(3, result.Maximum);
        Assert.AreEqual(-1.0, result.Average, 1e-6);
    }
}
