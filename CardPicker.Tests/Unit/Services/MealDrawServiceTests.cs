using CardPicker.Models;
using CardPicker.Services;
using Microsoft.Extensions.Logging;

namespace CardPicker.Tests.Unit.Services;

public sealed class MealDrawServiceTests
{
    [Fact]
    public async Task DrawAsync_WhenSelectedMealTypeIsMissing_ReturnsValidationFailedAndSkipsDependencies()
    {
        var mealCardService = new Mock<IMealCardService>(MockBehavior.Strict);
        var randomIndexProvider = new Mock<IRandomIndexProvider>(MockBehavior.Strict);
        var service = CreateService(mealCardService, randomIndexProvider);

        var result = await service.DrawAsync(new DrawRequest());

        Assert.Equal(DrawResultState.ValidationFailed, result.State);
        Assert.Null(result.CardId);
        Assert.Equal("請先選擇餐別。", result.Message);
        mealCardService.Verify(
            value => value.GetCardsAsync(It.IsAny<CardSearchCriteria?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        randomIndexProvider.Verify(value => value.GetIndex(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DrawAsync_WhenSelectedMealTypeHasNoCards_ReturnsEmptyPoolAndSkipsRandomProvider()
    {
        var mealCardService = CreateMealCardService(
        [
            CreateCard(
                "0195f2f4e7d47c6496ef0bbca4e6df6d",
                "火腿蛋吐司",
                MealType.Breakfast,
                "附近早餐店的招牌組合，五分鐘內可以外帶。"),
            CreateCard(
                "0195f2f5a1b67d1f9a2d4c9e1f0a2b3c",
                "紅燒牛肉麵",
                MealType.Lunch,
                "湯頭濃郁，適合想吃熱食又需要飽足感的中午。"),
        ]);
        var randomIndexProvider = new Mock<IRandomIndexProvider>(MockBehavior.Strict);
        var service = CreateService(mealCardService, randomIndexProvider);

        var result = await service.DrawAsync(
            new DrawRequest
            {
                SelectedMealType = MealType.Dinner,
            });

        Assert.Equal(DrawResultState.EmptyPool, result.State);
        Assert.Null(result.CardId);
        Assert.Equal("目前沒有可抽取的餐點。", result.Message);
        randomIndexProvider.Verify(value => value.GetIndex(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DrawAsync_WhenMealTypeIsSelected_ReturnsCardOnlyFromRequestedMealType()
    {
        var dinnerCardA = CreateCard(
            "0195f2f6c0d57f68851fd2efab123456",
            "烤鮭魚定食",
            MealType.Dinner,
            "有主菜、白飯和味噌湯，適合下班後快速吃完。");
        var dinnerCardB = CreateCard(
            "0195f2f74e1b7d348b89c0d1ef654321",
            "麻油雞麵線",
            MealType.Dinner,
            "熱湯配麵線，天氣轉涼時特別適合。");
        var mealCardService = CreateMealCardService(
        [
            CreateCard(
                "0195f2f4e7d47c6496ef0bbca4e6df6d",
                "火腿蛋吐司",
                MealType.Breakfast,
                "附近早餐店的招牌組合，五分鐘內可以外帶。"),
            dinnerCardA,
            CreateCard(
                "0195f2f5a1b67d1f9a2d4c9e1f0a2b3c",
                "紅燒牛肉麵",
                MealType.Lunch,
                "湯頭濃郁，適合想吃熱食又需要飽足感的中午。"),
            dinnerCardB,
        ]);
        var randomIndexProvider = new Mock<IRandomIndexProvider>(MockBehavior.Strict);
        randomIndexProvider
            .Setup(value => value.GetIndex(2))
            .Returns(1);
        var service = CreateService(mealCardService, randomIndexProvider);

        var result = await service.DrawAsync(
            new DrawRequest
            {
                SelectedMealType = MealType.Dinner,
            });

        Assert.Equal(DrawResultState.Drawn, result.State);
        Assert.Equal(dinnerCardB.Id, result.CardId);
        Assert.DoesNotContain(
            result.CardId,
            new[]
            {
                "0195f2f4e7d47c6496ef0bbca4e6df6d",
                "0195f2f5a1b67d1f9a2d4c9e1f0a2b3c",
            });
        randomIndexProvider.Verify(value => value.GetIndex(2), Times.Once);
    }

    [Fact]
    public async Task DrawAsync_WhenRandomIndexProviderReturnsEveryCandidateIndex_CanDrawEveryCandidate()
    {
        var lunchCardA = CreateCard(
            "0195f2f4e7d47c6496ef0bbca4e6df6d",
            "雞腿便當",
            MealType.Lunch,
            "主菜、配菜和白飯一次解決。");
        var lunchCardB = CreateCard(
            "0195f2f5a1b67d1f9a2d4c9e1f0a2b3c",
            "牛肉燴飯",
            MealType.Lunch,
            "濃郁醬汁搭配白飯，很適合趕時間的中午。");
        var lunchCardC = CreateCard(
            "0195f2f63c897b2491c88d6e5f4a3210",
            "日式咖哩飯",
            MealType.Lunch,
            "辛香溫和、份量穩定，適合日常午餐。");
        var mealCardService = CreateMealCardService([lunchCardA, lunchCardB, lunchCardC]);
        var randomIndexProvider = new Mock<IRandomIndexProvider>(MockBehavior.Strict);
        randomIndexProvider
            .SetupSequence(value => value.GetIndex(3))
            .Returns(0)
            .Returns(1)
            .Returns(2);
        var service = CreateService(mealCardService, randomIndexProvider);
        var request = new DrawRequest
        {
            SelectedMealType = MealType.Lunch,
        };

        var firstResult = await service.DrawAsync(request);
        var secondResult = await service.DrawAsync(request);
        var thirdResult = await service.DrawAsync(request);

        Assert.Collection(
            new[] { firstResult, secondResult, thirdResult },
            result =>
            {
                Assert.Equal(DrawResultState.Drawn, result.State);
                Assert.Equal(lunchCardA.Id, result.CardId);
            },
            result =>
            {
                Assert.Equal(DrawResultState.Drawn, result.State);
                Assert.Equal(lunchCardB.Id, result.CardId);
            },
            result =>
            {
                Assert.Equal(DrawResultState.Drawn, result.State);
                Assert.Equal(lunchCardC.Id, result.CardId);
            });
    }

    [Fact]
    public async Task DrawAsync_WhenInvokedAcrossSupportedOutcomes_ReturnsExpectedResultStates()
    {
        var validationService = CreateService(
            new Mock<IMealCardService>(MockBehavior.Strict),
            new Mock<IRandomIndexProvider>(MockBehavior.Strict));

        var emptyPoolMealCardService = CreateMealCardService([]);
        var emptyPoolRandomIndexProvider = new Mock<IRandomIndexProvider>(MockBehavior.Strict);
        var emptyPoolService = CreateService(emptyPoolMealCardService, emptyPoolRandomIndexProvider);

        var drawnCard = CreateCard(
            "0195f2f63c897b2491c88d6e5f4a3210",
            "日式咖哩飯",
            MealType.Lunch,
            "辛香溫和、份量穩定，適合日常午餐。");
        var drawnMealCardService = CreateMealCardService([drawnCard]);
        var drawnRandomIndexProvider = new Mock<IRandomIndexProvider>(MockBehavior.Strict);
        drawnRandomIndexProvider
            .Setup(value => value.GetIndex(1))
            .Returns(0);
        var drawnService = CreateService(drawnMealCardService, drawnRandomIndexProvider);

        var results = new[]
        {
            await validationService.DrawAsync(new DrawRequest()),
            await emptyPoolService.DrawAsync(new DrawRequest { SelectedMealType = MealType.Breakfast }),
            await drawnService.DrawAsync(new DrawRequest { SelectedMealType = MealType.Lunch }),
        };

        Assert.Collection(
            results,
            result => Assert.Equal(DrawResultState.ValidationFailed, result.State),
            result => Assert.Equal(DrawResultState.EmptyPool, result.State),
            result => Assert.Equal(DrawResultState.Drawn, result.State));
    }

    private static MealDrawService CreateService(
        Mock<IMealCardService> mealCardService,
        Mock<IRandomIndexProvider> randomIndexProvider)
    {
        ArgumentNullException.ThrowIfNull(mealCardService);
        ArgumentNullException.ThrowIfNull(randomIndexProvider);

        return new MealDrawService(
            mealCardService.Object,
            randomIndexProvider.Object,
            Mock.Of<ILogger<MealDrawService>>());
    }

    private static Mock<IMealCardService> CreateMealCardService(IReadOnlyList<MealCard> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);

        var mealCardService = new Mock<IMealCardService>(MockBehavior.Strict);
        mealCardService
            .Setup(value => value.GetCardsAsync(It.IsAny<CardSearchCriteria?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CardSearchCriteria? criteria, CancellationToken _) =>
            {
                IEnumerable<MealCard> filteredCards = cards;
                if (criteria?.MealType is MealType mealType)
                {
                    filteredCards = filteredCards.Where(card => card.MealType == mealType);
                }

                return filteredCards.ToArray();
            });
        return mealCardService;
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
