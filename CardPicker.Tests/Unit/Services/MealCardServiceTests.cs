using System.IO;
using CardPicker.Models;
using CardPicker.Services;
using Microsoft.Extensions.Logging;

namespace CardPicker.Tests.Unit.Services;

public sealed class MealCardServiceTests
{
    [Fact]
    public async Task GetCardsAsync_WhenCriteriaIncludesMealTypeAndKeyword_ReturnsOnlyMatchingCards()
    {
        var breakfastCard = CreateCard(
            "0195f2f4e7d47c6496ef0bbca4e6df6d",
            "火腿蛋吐司",
            MealType.Breakfast,
            "附近早餐店的招牌組合，五分鐘內可以外帶。");
        var lunchCard = CreateCard(
            "0195f2f5a1b67d1f9a2d4c9e1f0a2b3c",
            "紅燒牛肉麵",
            MealType.Lunch,
            "湯頭濃郁，適合想吃熱食又需要飽足感的中午。");
        var dinnerCard = CreateCard(
            "0195f2f63c897b2491c88d6e5f4a3210",
            "鮭魚便當",
            MealType.Dinner,
            "有主菜、青菜和白飯，適合下班後快速解決晚餐。");
        var repository = CreateRepository([breakfastCard, lunchCard, dinnerCard]);
        var service = CreateService(repository);

        var cards = await service.GetCardsAsync(
            new CardSearchCriteria
            {
                Keyword = "  便當  ",
                MealType = MealType.Dinner,
            });

        var card = Assert.Single(cards);
        Assert.Equal(dinnerCard.Id, card.Id);
        Assert.Equal(dinnerCard.Name, card.Name);
    }

    [Fact]
    public async Task GetCardsAsync_WhenCriteriaIsInvalid_ThrowsAndSkipsRepository()
    {
        var repository = new Mock<IMealCardRepository>(MockBehavior.Strict);
        var service = CreateService(repository);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetCardsAsync(
                new CardSearchCriteria
                {
                    Keyword = new string('餐', CardSearchCriteria.MaxKeywordLength + 1),
                }));

        repository.Verify(
            value => value.LoadAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetCardByIdAsync_WhenIdUsesDifferentFormat_ReturnsMatchingCard()
    {
        var matchingCard = CreateCard(
            "0195f2f5a1b67d1f9a2d4c9e1f0a2b3c",
            "紅燒牛肉麵",
            MealType.Lunch,
            "湯頭濃郁，適合想吃熱食又需要飽足感的中午。");
        var repository = CreateRepository(
        [
            CreateCard(
                "0195f2f4e7d47c6496ef0bbca4e6df6d",
                "火腿蛋吐司",
                MealType.Breakfast,
                "附近早餐店的招牌組合，五分鐘內可以外帶。"),
            matchingCard,
        ]);
        var service = CreateService(repository);

        var card = await service.GetCardByIdAsync($"  {Guid.Parse(matchingCard.Id).ToString("D").ToUpperInvariant()}  ");

        Assert.NotNull(card);
        Assert.Equal(matchingCard.Id, card.Id);
        Assert.Equal(matchingCard.Name, card.Name);
    }

    [Fact]
    public async Task GetCardByIdAsync_WhenIdIsBlank_ThrowsAndSkipsRepository()
    {
        var repository = new Mock<IMealCardRepository>(MockBehavior.Strict);
        var service = CreateService(repository);

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetCardByIdAsync("   "));

        repository.Verify(
            value => value.LoadAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetCardsAsync_WhenLibraryContainsDuplicateNormalizedContent_ThrowsInvalidDataException()
    {
        var repository = CreateRepository(
        [
            CreateCard(
                "0195f2f4e7d47c6496ef0bbca4e6df6d",
                "牛肉麵",
                MealType.Lunch,
                "濃郁湯頭\r\n適合加麵"),
            CreateCard(
                "0195f2f5a1b67d1f9a2d4c9e1f0a2b3c",
                "  牛肉麵  ",
                MealType.Lunch,
                "濃郁湯頭\n適合加麵  "),
        ]);
        var service = CreateService(repository);

        await Assert.ThrowsAsync<InvalidDataException>(() => service.GetCardsAsync());
    }

    private static MealCardService CreateService(Mock<IMealCardRepository> repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        return new MealCardService(
            repository.Object,
            Mock.Of<ILogger<MealCardService>>());
    }

    private static Mock<IMealCardRepository> CreateRepository(IReadOnlyList<MealCard> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);

        var repository = new Mock<IMealCardRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CardLibraryDocument.CreateDefault(cards));
        return repository;
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
