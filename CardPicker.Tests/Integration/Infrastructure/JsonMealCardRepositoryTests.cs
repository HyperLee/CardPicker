using System.IO;
using CardPicker.Models;
using CardPicker.Options;
using CardPicker.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CardPicker.Tests.Integration.Infrastructure;

public sealed class JsonMealCardRepositoryTests
{
[Fact]
    public async Task LoadAsync_WhenStorageIsMissing_SeedsDefaultCardsInIsolatedDirectory()
    {
        using var factory = new CardPickerWebApplicationFactory();
        var repository = CreateRepository(factory.CardDataDirectory);

        var document = await repository.LoadAsync();

        Assert.True(File.Exists(factory.CardDataDirectory.CardsFilePath));
        Assert.Equal(CardLibraryDocument.CurrentSchemaVersion, document.SchemaVersion);
        Assert.Collection(
            document.Cards,
            card =>
            {
                Assert.Equal("0195f2f4e7d47c6496ef0bbca4e6df6d", card.Id);
                Assert.Equal("火腿蛋吐司", card.Name);
                Assert.Equal(MealType.Breakfast, card.MealType);
            },
            card =>
            {
                Assert.Equal("0195f2f5a1b67d1f9a2d4c9e1f0a2b3c", card.Id);
                Assert.Equal("紅燒牛肉麵", card.Name);
                Assert.Equal(MealType.Lunch, card.MealType);
            },
            card =>
            {
                Assert.Equal("0195f2f63c897b2491c88d6e5f4a3210", card.Id);
                Assert.Equal("鮭魚便當", card.Name);
                Assert.Equal(MealType.Dinner, card.MealType);
            });
        Assert.Contains("\"schemaVersion\": \"1.0\"", factory.CardDataDirectory.ReadCardsJson(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadAsync_WhenSeedDocumentAlreadyExists_LoadsExpectedSeedCardsFromIsolatedDirectory()
    {
        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(
            """
            {
              "schemaVersion": "1.0",
              "cards": [
                {
                  "id": "0195f2f4e7d47c6496ef0bbca4e6df6d",
                  "name": "火腿蛋吐司",
                  "mealType": "Breakfast",
                  "description": "附近早餐店的招牌組合，五分鐘內可以外帶。",
                  "createdAtUtc": "2024-01-01T08:00:00Z",
                  "updatedAtUtc": "2024-01-01T08:00:00Z"
                },
                {
                  "id": "0195f2f5a1b67d1f9a2d4c9e1f0a2b3c",
                  "name": "紅燒牛肉麵",
                  "mealType": "Lunch",
                  "description": "湯頭濃郁，適合想吃熱食又需要飽足感的中午。",
                  "createdAtUtc": "2024-01-01T08:05:00Z",
                  "updatedAtUtc": "2024-01-01T08:05:00Z"
                },
                {
                  "id": "0195f2f63c897b2491c88d6e5f4a3210",
                  "name": "鮭魚便當",
                  "mealType": "Dinner",
                  "description": "有主菜、青菜和白飯，適合下班後快速解決晚餐。",
                  "createdAtUtc": "2024-01-01T08:10:00Z",
                  "updatedAtUtc": "2024-01-01T08:10:00Z"
                }
              ]
            }
            """);
        var repository = CreateRepository(factory.CardDataDirectory);

        var document = await repository.LoadAsync();

        Assert.Equal(CardLibraryDocument.CurrentSchemaVersion, document.SchemaVersion);
        Assert.Collection(
            document.Cards,
            card =>
            {
                Assert.Equal("火腿蛋吐司", card.Name);
                Assert.Equal(MealType.Breakfast, card.MealType);
            },
            card =>
            {
                Assert.Equal("紅燒牛肉麵", card.Name);
                Assert.Equal(MealType.Lunch, card.MealType);
            },
            card =>
            {
                Assert.Equal("鮭魚便當", card.Name);
                Assert.Equal(MealType.Dinner, card.MealType);
            });
    }

    [Fact]
    public async Task LoadAsync_WhenStorageIsMissingAndSeedingIsDisabled_ThrowsFileNotFoundException()
    {
        using var factory = new CardPickerWebApplicationFactory();
        var repository = CreateRepository(
            factory.CardDataDirectory,
            new CardStorageOptions
            {
                RelativeFilePath = CardStorageOptions.DefaultRelativeFilePath,
                SchemaVersion = CardLibraryDocument.CurrentSchemaVersion,
                CreateSeedDataWhenMissing = false,
            });

        var exception = await Assert.ThrowsAsync<FileNotFoundException>(() => repository.LoadAsync());

        Assert.Equal(factory.CardDataDirectory.CardsFilePath, exception.FileName);
        Assert.False(File.Exists(factory.CardDataDirectory.CardsFilePath));
    }

    [Fact]
    public async Task SaveAsync_WhenDocumentIsValid_PersistsJsonAndCanBeLoadedAgain()
    {
        using var factory = new CardPickerWebApplicationFactory();
        var repository = CreateRepository(factory.CardDataDirectory);
        var document = CardLibraryDocument.CreateDefault(
        [
            CreateCard(
                "0195f2f4e7d47c6496ef0bbca4e6df6d",
                "豆漿蛋餅",
                MealType.Breakfast,
                "附近早餐店的經典組合。"),
            CreateCard(
                "0195f2f5a1b67d1f9a2d4c9e1f0a2b3c",
                "香煎雞腿便當",
                MealType.Dinner,
                "主餐與配菜一次搞定。"),
        ]);

        await repository.SaveAsync(document);
        var reloaded = await repository.LoadAsync();

        Assert.True(File.Exists(factory.CardDataDirectory.CardsFilePath));
        Assert.Empty(Directory.EnumerateFiles(factory.CardDataDirectory.DataDirectoryPath, "*.tmp", SearchOption.TopDirectoryOnly));
        Assert.Collection(
            reloaded.Cards,
            card =>
            {
                Assert.Equal("豆漿蛋餅", card.Name);
                Assert.Equal(MealType.Breakfast, card.MealType);
            },
            card =>
            {
                Assert.Equal("香煎雞腿便當", card.Name);
                Assert.Equal(MealType.Dinner, card.MealType);
            });
        Assert.Contains("\"mealType\": \"Dinner\"", factory.CardDataDirectory.ReadCardsJson(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadAsync_WhenStoredDocumentContainsDuplicateIds_ThrowsInvalidDataException()
    {
        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(
            """
            {
              "schemaVersion": "1.0",
              "cards": [
                {
                  "id": "0195f2f4e7d47c6496ef0bbca4e6df6d",
                  "name": "火腿蛋吐司",
                  "mealType": "Breakfast",
                  "description": "附近早餐店的招牌組合，五分鐘內可以外帶。",
                  "createdAtUtc": "2024-01-01T08:00:00Z",
                  "updatedAtUtc": "2024-01-01T08:00:00Z"
                },
                {
                  "id": "0195f2f4e7d47c6496ef0bbca4e6df6d",
                  "name": "紅燒牛肉麵",
                  "mealType": "Lunch",
                  "description": "湯頭濃郁，適合想吃熱食又需要飽足感的中午。",
                  "createdAtUtc": "2024-01-01T08:05:00Z",
                  "updatedAtUtc": "2024-01-01T08:05:00Z"
                }
              ]
            }
            """);
        var repository = CreateRepository(factory.CardDataDirectory);

        await Assert.ThrowsAsync<InvalidDataException>(() => repository.LoadAsync());
    }

    [Fact]
    public async Task LoadAsync_WhenStoredDocumentUsesUnexpectedSchema_ThrowsInvalidDataException()
    {
        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(
            """
            {
              "schemaVersion": "2.0",
              "cards": [
                {
                  "id": "0195f2f4e7d47c6496ef0bbca4e6df6d",
                  "name": "火腿蛋吐司",
                  "mealType": "Breakfast",
                  "description": "附近早餐店的招牌組合，五分鐘內可以外帶。",
                  "createdAtUtc": "2024-01-01T08:00:00Z",
                  "updatedAtUtc": "2024-01-01T08:00:00Z"
                }
              ]
            }
            """);
        var repository = CreateRepository(factory.CardDataDirectory);

        await Assert.ThrowsAsync<InvalidDataException>(() => repository.LoadAsync());
    }

    private static JsonMealCardRepository CreateRepository(
        TestCardDataDirectory cardDataDirectory,
        CardStorageOptions? storageOptions = null)
    {
        ArgumentNullException.ThrowIfNull(cardDataDirectory);

        var hostEnvironment = new Mock<IHostEnvironment>(MockBehavior.Strict);
        hostEnvironment
            .SetupGet(value => value.ContentRootPath)
            .Returns(cardDataDirectory.RootPath);

        return new JsonMealCardRepository(
            Microsoft.Extensions.Options.Options.Create(storageOptions ?? new CardStorageOptions()),
            hostEnvironment.Object,
            Mock.Of<ILogger<JsonMealCardRepository>>());
    }

    private static MealCard CreateCard(
        string id,
        string name,
        MealType mealType,
        string description,
        DateTimeOffset? timestampUtc = null)
    {
        var normalizedTimestamp = timestampUtc ?? new DateTimeOffset(2024, 1, 1, 8, 0, 0, TimeSpan.Zero);

        return new MealCard
        {
            Id = id,
            Name = MealCard.NormalizeName(name),
            MealType = mealType,
            Description = MealCard.NormalizeDescription(description),
            CreatedAtUtc = normalizedTimestamp,
            UpdatedAtUtc = normalizedTimestamp,
        };
    }
}
