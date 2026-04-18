using CardPicker.Models;

namespace CardPicker.Services;

/// <summary>
/// Coordinates meal-type validation, candidate lookup, and random selection for home page draw requests.
/// </summary>
/// <example>
/// <code>
/// var result = await mealDrawService.DrawAsync(
///     new DrawRequest { SelectedMealType = MealType.Lunch },
///     cancellationToken);
/// </code>
/// </example>
public sealed class MealDrawService : IMealDrawService
{
    private const string ValidationFailedMessage = "請先選擇餐別。";
    private const string EmptyPoolMessage = "目前沒有可抽取的餐點。";
    private const string DrawnMessage = "已為您抽出今天的餐點。";

    private readonly IMealCardService _mealCardService;
    private readonly IRandomIndexProvider _randomIndexProvider;
    private readonly ILogger<MealDrawService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MealDrawService" /> class.
    /// </summary>
    /// <param name="mealCardService">The meal card query service.</param>
    /// <param name="randomIndexProvider">The provider used to select a uniform random index.</param>
    /// <param name="logger">The logger for draw outcomes.</param>
    public MealDrawService(
        IMealCardService mealCardService,
        IRandomIndexProvider randomIndexProvider,
        ILogger<MealDrawService> logger)
    {
        ArgumentNullException.ThrowIfNull(mealCardService);
        ArgumentNullException.ThrowIfNull(randomIndexProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _mealCardService = mealCardService;
        _randomIndexProvider = randomIndexProvider;
        _logger = logger;
    }

    /// <summary>
    /// Validates the request, loads candidate cards for the selected meal type, and returns one random draw result.
    /// </summary>
    /// <param name="request">The draw request from the home page.</param>
    /// <param name="cancellationToken">The cancellation token for the draw operation.</param>
    /// <returns>
    /// A <see cref="DrawResult" /> whose state indicates whether validation failed, the candidate pool was empty,
    /// or a meal card was drawn successfully.
    /// </returns>
    /// <example>
    /// <code>
    /// var result = await mealDrawService.DrawAsync(
    ///     new DrawRequest { SelectedMealType = MealType.Dinner },
    ///     cancellationToken);
    /// </code>
    /// </example>
    public async Task<DrawResult> DrawAsync(DrawRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetValidatedMealType(request.SelectedMealType, out var mealType))
        {
            _logger.LogInformation("Meal draw validation failed because no valid meal type was selected.");
            return new DrawResult
            {
                State = DrawResultState.ValidationFailed,
                Message = ValidationFailedMessage,
            };
        }

        var candidateCards = await _mealCardService.GetCardsAsync(
                new CardSearchCriteria { MealType = mealType },
                cancellationToken)
            .ConfigureAwait(false);

        if (candidateCards.Count == 0)
        {
            _logger.LogInformation("Meal draw found no candidates for meal type {MealType}.", mealType);
            return new DrawResult
            {
                State = DrawResultState.EmptyPool,
                MealType = mealType,
                Message = EmptyPoolMessage,
            };
        }

        var selectedIndex = _randomIndexProvider.GetIndex(candidateCards.Count);
        var selectedCard = candidateCards[selectedIndex];

        _logger.LogInformation(
            "Meal draw selected card {CardId} for meal type {MealType} from {CandidateCount} candidates.",
            selectedCard.Id,
            selectedCard.MealType,
            candidateCards.Count);

        return new DrawResult
        {
            State = DrawResultState.Drawn,
            CardId = selectedCard.Id,
            CardName = selectedCard.Name,
            MealType = selectedCard.MealType,
            Description = selectedCard.Description,
            Message = DrawnMessage,
        };
    }

    private static bool TryGetValidatedMealType(MealType? selectedMealType, out MealType mealType)
    {
        if (selectedMealType.HasValue && Enum.IsDefined(selectedMealType.Value))
        {
            mealType = selectedMealType.Value;
            return true;
        }

        mealType = default;
        return false;
    }
}
