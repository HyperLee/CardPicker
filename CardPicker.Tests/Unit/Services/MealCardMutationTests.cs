using System.IO;
using System.Reflection;
using CardPicker.Models;
using CardPicker.Services;
using Microsoft.Extensions.Logging;

namespace CardPicker.Tests.Unit.Services;

public sealed class MealCardMutationTests
{
    [Theory]
    [MemberData(nameof(GetInvalidCreateInputs))]
    public async Task CreateCardAsync_WhenRequiredFieldIsMissing_ThrowsArgumentException(
        string name,
        MealType mealType,
        string description)
    {
        var repository = new TrackingMealCardRepository();
        var service = CreateService(repository);
        var createMethod = GetCreateMethod();

        var exception = await Record.ExceptionAsync(
            () => InvokeAsync(
                service,
                createMethod,
                name,
                mealType,
                description));

        Assert.NotNull(exception);
        Assert.IsAssignableFrom<ArgumentException>(exception);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task CreateCardAsync_WhenNormalizedContentDuplicatesExistingCard_ThrowsInvalidDataException()
    {
        var existingCard = CreateCard(
            "0195f2f4e7d47c6496ef0bbca4e6df6d",
            "Burger",
            MealType.Lunch,
            "Juicy patty\r\nWith cheese");
        var repository = new TrackingMealCardRepository([existingCard]);
        var service = CreateService(repository);
        var createMethod = GetCreateMethod();

        await Assert.ThrowsAsync<InvalidDataException>(
            () => InvokeAsync(
                service,
                createMethod,
                "  burger  ",
                MealType.Lunch,
                "Juicy patty\nWith cheese  "));

        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task EditCardAsync_WhenUpdatingCard_PreservesImmutableId()
    {
        var originalCard = CreateCard(
            "0195f2f5a1b67d1f9a2d4c9e1f0a2b3c",
            "Chicken Rice",
            MealType.Lunch,
            "Simple lunch option.");
        var repository = new TrackingMealCardRepository([originalCard]);
        var service = CreateService(repository);
        var editMethod = GetEditMethod();

        await InvokeAsync(
            service,
            editMethod,
            originalCard.Id,
            "  Chicken Rice Deluxe  ",
            MealType.Dinner,
            "Simple lunch option.\r\nNow with sides.");

        var reloadedCard = await service.GetCardByIdAsync(originalCard.Id);

        Assert.NotNull(reloadedCard);
        Assert.Equal(originalCard.Id, reloadedCard.Id);
        Assert.Equal(originalCard.CreatedAtUtc, reloadedCard.CreatedAtUtc);
        Assert.Equal("Chicken Rice Deluxe", reloadedCard.Name);
        Assert.Equal(MealType.Dinner, reloadedCard.MealType);
        Assert.Equal("Simple lunch option.\nNow with sides.", reloadedCard.Description);
        Assert.True(reloadedCard.UpdatedAtUtc >= originalCard.UpdatedAtUtc);
        Assert.Equal(1, repository.SaveCalls);
    }

    [Fact]
    public async Task EditCardAsync_WhenUpdatedContentCollidesWithExistingCard_ThrowsInvalidDataException()
    {
        var existingCard = CreateCard(
            "0195f2f4e7d47c6496ef0bbca4e6df6d",
            "Burger",
            MealType.Lunch,
            "Juicy patty\r\nWith cheese");
        var editedCard = CreateCard(
            "0195f2f63c897b2491c88d6e5f4a3210",
            "Salad Bowl",
            MealType.Dinner,
            "Fresh greens and dressing.");
        var repository = new TrackingMealCardRepository([existingCard, editedCard]);
        var service = CreateService(repository);
        var editMethod = GetEditMethod();

        await Assert.ThrowsAsync<InvalidDataException>(
            () => InvokeAsync(
                service,
                editMethod,
                editedCard.Id,
                "  burger  ",
                MealType.Lunch,
                "Juicy patty\nWith cheese  "));

        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task DeleteCardAsync_WhenCardIsDeleted_RemovesItFromSearchAndGetFlows()
    {
        var deletedCard = CreateCard(
            "0195f2f4e7d47c6496ef0bbca4e6df6d",
            "Burger",
            MealType.Lunch,
            "Juicy patty with cheese.");
        var remainingCard = CreateCard(
            "0195f2f5a1b67d1f9a2d4c9e1f0a2b3c",
            "Pasta",
            MealType.Dinner,
            "Tomato sauce and basil.");
        var repository = new TrackingMealCardRepository([deletedCard, remainingCard]);
        var service = CreateService(repository);
        var deleteMethod = GetDeleteMethod();

        await InvokeAsync(service, deleteMethod, deletedCard.Id);

        var loadedCard = await service.GetCardByIdAsync(deletedCard.Id);
        var searchResults = await service.GetCardsAsync(
            new CardSearchCriteria
            {
                Keyword = "burger",
            });
        var allCards = await service.GetCardsAsync();

        Assert.Null(loadedCard);
        Assert.Empty(searchResults);
        var remaining = Assert.Single(allCards);
        Assert.Equal(remainingCard.Id, remaining.Id);
        Assert.Equal(1, repository.SaveCalls);
    }

    public static TheoryData<string, MealType, string> GetInvalidCreateInputs()
    {
        return new TheoryData<string, MealType, string>
        {
            { "   ", MealType.Lunch, "Valid description" },
            { "Valid name", (MealType)999, "Valid description" },
            { "Valid name", MealType.Lunch, "   " },
        };
    }

    private static MealCardService CreateService(TrackingMealCardRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        return new MealCardService(
            repository,
            Mock.Of<ILogger<MealCardService>>());
    }

    private static MethodInfo GetCreateMethod() => GetRequiredMethod(
        typeof(MealCardService),
        "CreateCardAsync",
        [typeof(string), typeof(MealType), typeof(string)],
        [typeof(string), typeof(MealType), typeof(string), typeof(CancellationToken)]);

    private static MethodInfo GetEditMethod() => GetRequiredMethod(
        typeof(MealCardService),
        "EditCardAsync",
        [typeof(string), typeof(string), typeof(MealType), typeof(string)],
        [typeof(string), typeof(string), typeof(MealType), typeof(string), typeof(CancellationToken)],
        alternativeMethodName: "UpdateCardAsync");

    private static MethodInfo GetDeleteMethod() => GetRequiredMethod(
        typeof(MealCardService),
        "DeleteCardAsync",
        [typeof(string)],
        [typeof(string), typeof(CancellationToken)]);

    private static MethodInfo GetRequiredMethod(
        Type serviceType,
        string methodName,
        Type[] parametersWithoutCancellation,
        Type[] parametersWithCancellation,
        string? alternativeMethodName = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(methodName);
        ArgumentNullException.ThrowIfNull(parametersWithoutCancellation);
        ArgumentNullException.ThrowIfNull(parametersWithCancellation);

        var candidates = serviceType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(
                method =>
                    string.Equals(method.Name, methodName, StringComparison.Ordinal)
                    || string.Equals(method.Name, alternativeMethodName, StringComparison.Ordinal))
            .Where(method => typeof(Task).IsAssignableFrom(method.ReturnType))
            .ToArray();

        var methodInfo = candidates.FirstOrDefault(
                             method => HasParameterTypes(method, parametersWithCancellation))
                         ?? candidates.FirstOrDefault(
                             method => HasParameterTypes(method, parametersWithoutCancellation));
        var expectedMethodNames = string.IsNullOrWhiteSpace(alternativeMethodName)
            ? methodName
            : $"{methodName} (or {alternativeMethodName})";

        Assert.True(
            methodInfo is not null,
            $"Expected {serviceType.Name} to define {expectedMethodNames} with the mutation signature required by Phase 5 tests.");

        return methodInfo;
    }

    private static async Task InvokeAsync(
        MealCardService service,
        MethodInfo method,
        params object[] arguments)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(arguments);

        var parameters = method.GetParameters();
        var invocationArguments = parameters.Length == arguments.Length + 1
            ? [.. arguments, CancellationToken.None]
            : arguments;

        var result = method.Invoke(service, invocationArguments);
        var task = Assert.IsAssignableFrom<Task>(result);
        await task.ConfigureAwait(false);
    }

    private static bool HasParameterTypes(MethodInfo method, Type[] expectedParameterTypes)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(expectedParameterTypes);

        var parameters = method.GetParameters();
        if (parameters.Length != expectedParameterTypes.Length)
        {
            return false;
        }

        for (var index = 0; index < parameters.Length; index++)
        {
            if (parameters[index].ParameterType != expectedParameterTypes[index])
            {
                return false;
            }
        }

        return true;
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

    private sealed class TrackingMealCardRepository : IMealCardRepository
    {
        public TrackingMealCardRepository(IReadOnlyList<MealCard>? cards = null)
        {
            Document = CardLibraryDocument.CreateDefault(cards ?? []);
        }

        public CardLibraryDocument Document { get; private set; }

        public int SaveCalls { get; private set; }

        public Task<CardLibraryDocument> LoadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Document);
        }

        public Task SaveAsync(CardLibraryDocument document, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(document);

            SaveCalls++;
            Document = document;
            return Task.CompletedTask;
        }
    }
}
