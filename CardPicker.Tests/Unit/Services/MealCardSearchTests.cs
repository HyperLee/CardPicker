using CardPicker.Models;
using CardPicker.Services;
using Microsoft.Extensions.Logging;

namespace CardPicker.Tests.Unit.Services;

public sealed class MealCardSearchTests
{
    [Fact]
    public async Task GetCardsAsync_WhenKeywordMatchesPartiallyIgnoringCase_ReturnsMatchingCards()
    {
        var matchingCardA = CreateCard(
            "0195f2f4e7d47c6496ef0bbca4e6df6d",
            "Spicy Beef Bowl",
            MealType.Lunch,
            "Savory sliced beef with rice.");
        var matchingCardB = CreateCard(
            "0195f2f5a1b67d1f9a2d4c9e1f0a2b3c",
            "Beef Noodle Soup",
            MealType.Dinner,
            "Rich broth with braised beef.");
        var repository = CreateRepository(
        [
            matchingCardA,
            CreateCard(
                "0195f2f63c897b2491c88d6e5f4a3210",
                "Chicken Caesar Wrap",
                MealType.Lunch,
                "A quick lunch option with crisp lettuce."),
            matchingCardB,
        ]);
        var service = CreateService(repository);

        var cards = await service.GetCardsAsync(
            new CardSearchCriteria
            {
                Keyword = "  bEeF  ",
            });

        Assert.Collection(
            cards,
            card => Assert.Equal(matchingCardA.Id, card.Id),
            card => Assert.Equal(matchingCardB.Id, card.Id));
    }

    [Fact]
    public async Task GetCardsAsync_WhenOnlyMealTypeIsProvided_ReturnsOnlyCardsForThatMealType()
    {
        var matchingCardA = CreateCard(
            "0195f2f74e1b7d348b89c0d1ef654321",
            "Turkey Club Sandwich",
            MealType.Lunch,
            "Stacked sandwich for a filling lunch.");
        var matchingCardB = CreateCard(
            "0195f2f86f2c7f0bb5a1d2e3c4b56789",
            "Mushroom Risotto",
            MealType.Lunch,
            "Creamy rice with mushrooms and cheese.");
        var repository = CreateRepository(
        [
            CreateCard(
                "0195f2f97a3d7c44af12bc34de567890",
                "Berry Yogurt Bowl",
                MealType.Breakfast,
                "Fresh fruit with yogurt and granola."),
            matchingCardA,
            matchingCardB,
            CreateCard(
                "0195f2fa8b4e7d55b012cd34ef678901",
                "Garlic Butter Salmon",
                MealType.Dinner,
                "Pan-seared salmon with buttery sauce."),
        ]);
        var service = CreateService(repository);

        var cards = await service.GetCardsAsync(
            new CardSearchCriteria
            {
                MealType = MealType.Lunch,
            });

        Assert.Collection(
            cards,
            card => Assert.Equal(matchingCardA.Id, card.Id),
            card => Assert.Equal(matchingCardB.Id, card.Id));
    }

    [Fact]
    public async Task GetCardsAsync_WhenKeywordAndMealTypeAreProvided_ReturnsOnlyCardsMatchingBothFilters()
    {
        var matchingCard = CreateCard(
            "0195f2fb9c5f7e66c123de45f0891234",
            "Beef Fried Rice",
            MealType.Dinner,
            "Wok-fried rice with sliced beef.");
        var repository = CreateRepository(
        [
            CreateCard(
                "0195f2fcad607f77d234ef5601902345",
                "Beef Burrito",
                MealType.Lunch,
                "Portable burrito packed with seasoned beef."),
            matchingCard,
            CreateCard(
                "0195f2fdbe717088e345f0672a013456",
                "Vegetable Fried Rice",
                MealType.Dinner,
                "Dinner rice bowl without meat."),
        ]);
        var service = CreateService(repository);

        var cards = await service.GetCardsAsync(
            new CardSearchCriteria
            {
                Keyword = "  bEEF  ",
                MealType = MealType.Dinner,
            });

        var card = Assert.Single(cards);
        Assert.Equal(matchingCard.Id, card.Id);
        Assert.Equal(matchingCard.Name, card.Name);
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
