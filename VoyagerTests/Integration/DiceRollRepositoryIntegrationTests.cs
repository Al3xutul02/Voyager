using BusinessLogic.Enums.Types;
using BusinessLogic.Json;
using BusinessLogic.Json.Models;
using Newtonsoft.Json;
using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories;

namespace VoyagerTests.Integration;

/// <summary>
/// DiceRoll repository tests that run against a real MySQL instance (via Testcontainers).
/// Validates the <c>dice_rolls</c> table migration, foreign key relation to <c>users</c>,
/// auto-increment <c>ulong</c> primary key, and Pomelo LINQ-to-SQL translation.
/// </summary>
[TestClass]
[DoNotParallelize]
[TestCategory("Integration")]
public class DiceRollRepositoryIntegrationTests
{
    private const ulong UserSnowflake = 1320120043694194700UL;

    private static string DefaultUserSettings() =>
        JsonConvert.SerializeObject(new UserSettings(Color.Teal), VoyagerJsonSettings.Default);

    [ClassInitialize]
    public static async Task StartContainer(TestContext _) => await MySqlContainerFixture.StartAsync();

    [ClassCleanup]
    public static async Task StopContainer() => await MySqlContainerFixture.StopAsync();

    [TestInitialize]
    public async Task ResetDatabase() => await MySqlContainerFixture.ResetAsync();

    [TestMethod]
    public async Task AddAsync_WithForeignKeyUser_GeneratesIdAndRoundTripsThroughMySql()
    {
        ulong generatedId;

        await using (var writeContext = MySqlContainerFixture.CreateContext())
        {
            var userRepo = new UserRepository(writeContext);
            await userRepo.AddAsync(new User
            {
                Id = UserSnowflake,
                Name = "Ezren",
                Settings = DefaultUserSettings()
            });
            await userRepo.SaveAsync();

            var diceRepo = new DiceRollRepository(writeContext);
            var roll = new DiceRoll
            {
                UserId = UserSnowflake,
                Name = "Fireball",
                Formula = "(6d6)[fire]"
            };
            await diceRepo.AddAsync(roll);
            await diceRepo.SaveAsync();
            generatedId = roll.Id;
        }

        Assert.IsGreaterThan(0UL, generatedId);

        await using var readContext = MySqlContainerFixture.CreateContext();
        var stored = await new DiceRollRepository(readContext)
            .GetByIdAsync(generatedId, IncludeBehavior.NoInclude);

        Assert.IsNotNull(stored);
        Assert.AreEqual(UserSnowflake, stored!.UserId);
        Assert.AreEqual("Fireball", stored.Name);
        Assert.AreEqual("(6d6)[fire]", stored.Formula);
    }

    [TestMethod]
    public async Task GetByUserIdAndNameAndFormula_TranslateToSql_ReturnExpectedRows()
    {
        await using var context = MySqlContainerFixture.CreateContext();
        var userRepo = new UserRepository(context);
        await userRepo.AddAsync(new User { Id = 1, Name = "Kyra", Settings = DefaultUserSettings() });
        await userRepo.AddAsync(new User { Id = 2, Name = "Merisiel", Settings = DefaultUserSettings() });
        await userRepo.SaveAsync();

        var diceRepo = new DiceRollRepository(context);
        await diceRepo.AddAsync(new DiceRoll { UserId = 1, Name = "Heal", Formula = "2d8+16" });
        await diceRepo.AddAsync(new DiceRoll { UserId = 1, Name = "Scimitar", Formula = "1d6+3" });
        await diceRepo.AddAsync(new DiceRoll { UserId = 2, Name = "Rapier", Formula = "1d6+4" });
        await diceRepo.SaveAsync();

        var kyraRolls = (await diceRepo.GetByUserIdAsync(1, IncludeBehavior.NoInclude)).ToList();
        Assert.HasCount(2, kyraRolls);

        var byName = await diceRepo.GetByUserAndNameAsync(1, "Heal", IncludeBehavior.NoInclude);
        Assert.IsNotNull(byName);
        Assert.AreEqual("2d8+16", byName!.Formula);

        var byFormula = await diceRepo.GetByUserAndFormulaAsync(1, "1d6+3", IncludeBehavior.NoInclude);
        Assert.IsNotNull(byFormula);
        Assert.AreEqual("Scimitar", byFormula!.Name);
    }

    [TestMethod]
    public async Task CascadeDelete_WhenUserDeleted_DeletesOwnedDiceRolls()
    {
        await using (var seedContext = MySqlContainerFixture.CreateContext())
        {
            var userRepo = new UserRepository(seedContext);
            await userRepo.AddAsync(new User { Id = 1, Name = "Valeros", Settings = DefaultUserSettings() });
            await userRepo.SaveAsync();

            var diceRepo = new DiceRollRepository(seedContext);
            await diceRepo.AddAsync(new DiceRoll { UserId = 1, Name = "Longsword", Formula = "1d8+4" });
            await diceRepo.SaveAsync();
        }

        await using (var deleteContext = MySqlContainerFixture.CreateContext())
        {
            var userRepo = new UserRepository(deleteContext);
            var user = await userRepo.GetByIdAsync(1, IncludeBehavior.NoInclude);
            userRepo.Delete(user!);
            await userRepo.SaveAsync();
        }

        await using var verifyContext = MySqlContainerFixture.CreateContext();
        var remainingRolls = await new DiceRollRepository(verifyContext).GetByUserIdAsync(1, IncludeBehavior.NoInclude);
        Assert.IsEmpty(remainingRolls);
    }
}
