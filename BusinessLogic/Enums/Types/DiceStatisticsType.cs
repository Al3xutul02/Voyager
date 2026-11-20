using DSharpPlus.SlashCommands;

namespace BusinessLogic.Enums.Types;

/// <summary>
/// Specifies the statistical calculation mode for a dice roll command instead
/// of performing a random roll.
/// </summary>
public enum DiceStatisticsType
{
    /// <summary>Calculates the minimum possible result of the roll.</summary>
    [ChoiceName("Minimum")]
    Minimum,

    /// <summary>Calculates the maximum possible result of the roll.</summary>
    [ChoiceName("Maximum")]
    Maximum,

    /// <summary>Calculates the expected value (average) of the roll.</summary>
    [ChoiceName("Average")]
    Average,

    /// <summary>Calculates the theoretical standard deviation of the roll.</summary>
    [ChoiceName("Standard Deviation")]
    StandardDeviation,

    /// <summary>Calculates all statistics (Minimum, Maximum, Average, and Standard Deviation) of the roll.</summary>
    [ChoiceName("All")]
    All
}
