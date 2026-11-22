using BusinessLogic.Enums.Types;
using BusinessLogic.Json;
using BusinessLogic.Json.Models;
using Newtonsoft.Json;
using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories;

namespace VoyagerTests.Integration;

/// <summary>
/// Character repository tests that run against a real MySQL instance (via Testcontainers).
/// Validates the <c>characters</c> table migration, foreign key relation to <c>users</c>,
/// auto-increment <c>ulong</c> primary key, decimal/text columns, and Pomelo LINQ-to-SQL translation.
/// </summary>
[TestClass]
[DoNotParallelize]
[TestCategory("Integration")]
public class CharacterRepositoryIntegrationTests
{
    private const ulong UserSnowflake = 1320120043694194700UL;

    private static string DefaultUserSettings() =>
        JsonConvert.SerializeObject(new UserSettings(Color.Teal), VoyagerJsonSettings.Default);

    private static string DefaultCharacterDetails() =>
        JsonConvert.SerializeObject(new CharacterDetails("Human", "Rogue", "3", "25", "Male"), VoyagerJsonSettings.Default);

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

            var charRepo = new CharacterRepository(writeContext);
            var character = new Character
            {
                UserId = UserSnowflake,
                Name = "Sunaki",
                Description = "A master thief from Ravounel.",
                Color = (int)Color.Purple,
                TokenImagePath = "characters/123/token.png",
                ActorImagePath = "characters/123/actor.png",
                CurrentGame = null,
                Gold = 15.75m,
                PathbuilderId = "99999",
                Details = DefaultCharacterDetails()
            };
            await charRepo.AddAsync(character);
            await charRepo.SaveAsync();
            generatedId = character.Id;
        }

        Assert.IsGreaterThan(0UL, generatedId);

        await using var readContext = MySqlContainerFixture.CreateContext();
        var stored = await new CharacterRepository(readContext)
            .GetByIdAsync(generatedId, IncludeBehavior.NoInclude);

        Assert.IsNotNull(stored);
        Assert.AreEqual(UserSnowflake, stored!.UserId);
        Assert.AreEqual("Sunaki", stored.Name);
        Assert.AreEqual("A master thief from Ravounel.", stored.Description);
        Assert.AreEqual((int)Color.Purple, stored.Color);
        Assert.AreEqual("characters/123/token.png", stored.TokenImagePath);
        Assert.AreEqual("characters/123/actor.png", stored.ActorImagePath);
        Assert.IsNull(stored.CurrentGame);
        Assert.AreEqual(15.75m, stored.Gold);
        Assert.AreEqual("99999", stored.PathbuilderId);
        Assert.IsFalse(string.IsNullOrWhiteSpace(stored.Details));
    }

    [TestMethod]
    public async Task GetByUserIdAndName_TranslateToSql_ReturnExpectedRows()
    {
        await using var context = MySqlContainerFixture.CreateContext();
        var userRepo = new UserRepository(context);
        await userRepo.AddAsync(new User { Id = 1, Name = "Kyra", Settings = DefaultUserSettings() });
        await userRepo.AddAsync(new User { Id = 2, Name = "Merisiel", Settings = DefaultUserSettings() });
        await userRepo.SaveAsync();

        var charRepo = new CharacterRepository(context);
        await charRepo.AddAsync(new Character
        {
            UserId = 1,
            Name = "Sarenrae's Light",
            Details = DefaultCharacterDetails(),
            TokenImagePath = "token1.png",
            ActorImagePath = "actor1.png"
        });
        await charRepo.AddAsync(new Character
        {
            UserId = 1,
            Name = "Dawnflower",
            Details = DefaultCharacterDetails(),
            TokenImagePath = "token2.png",
            ActorImagePath = "actor2.png"
        });
        await charRepo.AddAsync(new Character
        {
            UserId = 2,
            Name = "Shadowblade",
            Details = DefaultCharacterDetails(),
            TokenImagePath = "token3.png",
            ActorImagePath = "actor3.png"
        });
        await charRepo.SaveAsync();

        var kyraChars = (await charRepo.GetByUserIdAsync(1, IncludeBehavior.NoInclude)).ToList();
        Assert.HasCount(2, kyraChars);

        var count = await charRepo.GetCountByUserIdAsync(1);
        Assert.AreEqual(2, count);

        var byName = await charRepo.GetByUserAndNameAsync(1, "Dawnflower", IncludeBehavior.NoInclude);
        Assert.IsNotNull(byName);
        Assert.AreEqual("Dawnflower", byName!.Name);

        var byNameCaseInsensitive = await charRepo.GetByUserAndNameAsync(1, "dawnflower", IncludeBehavior.NoInclude);
        Assert.IsNotNull(byNameCaseInsensitive);
    }

    [TestMethod]
    public async Task CascadeDelete_WhenUserDeleted_DeletesOwnedCharacters()
    {
        await using (var seedContext = MySqlContainerFixture.CreateContext())
        {
            var userRepo = new UserRepository(seedContext);
            await userRepo.AddAsync(new User { Id = 1, Name = "Valeros", Settings = DefaultUserSettings() });
            await userRepo.SaveAsync();

            var charRepo = new CharacterRepository(seedContext);
            await charRepo.AddAsync(new Character
            {
                UserId = 1,
                Name = "Fighter Guy",
                Details = DefaultCharacterDetails(),
                TokenImagePath = "token.png",
                ActorImagePath = "actor.png"
            });
            await charRepo.SaveAsync();
        }

        await using (var deleteContext = MySqlContainerFixture.CreateContext())
        {
            var userRepo = new UserRepository(deleteContext);
            var user = await userRepo.GetByIdAsync(1, IncludeBehavior.NoInclude);
            userRepo.Delete(user!);
            await userRepo.SaveAsync();
        }

        await using var verifyContext = MySqlContainerFixture.CreateContext();
        var remainingChars = await new CharacterRepository(verifyContext).GetByUserIdAsync(1, IncludeBehavior.NoInclude);
        Assert.IsEmpty(remainingChars);
    }

    [TestMethod]
    public async Task IncludeBehavior_AllIncludes_LoadsUserNavigation()
    {
        ulong generatedId;

        await using (var context = MySqlContainerFixture.CreateContext())
        {
            var userRepo = new UserRepository(context);
            await userRepo.AddAsync(new User { Id = 10, Name = "Fumbus", Settings = DefaultUserSettings() });
            await userRepo.SaveAsync();

            var charRepo = new CharacterRepository(context);
            var character = new Character
            {
                UserId = 10,
                Name = "Bombmaker",
                Details = DefaultCharacterDetails(),
                TokenImagePath = "token.png",
                ActorImagePath = "actor.png"
            };
            await charRepo.AddAsync(character);
            await charRepo.SaveAsync();
            generatedId = character.Id;
        }

        await using var readContext = MySqlContainerFixture.CreateContext();
        var loaded = await new CharacterRepository(readContext)
            .GetByIdAsync(generatedId, IncludeBehavior.AllIncludes);

        Assert.IsNotNull(loaded);
        Assert.IsNotNull(loaded!.User);
        Assert.AreEqual("Fumbus", loaded.User.Name);
    }
}
