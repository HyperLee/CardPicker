using CardPicker.Models;

namespace CardPicker.Services;

/// <summary>
/// Encapsulates meal card lookup, search, and shared library validation rules.
/// </summary>
/// <example>
/// <code>
/// var lunchCards = await mealCardService.GetCardsAsync(
///     new CardSearchCriteria { MealType = MealType.Lunch },
///     cancellationToken);
/// </code>
/// </example>
public sealed class MealCardService : IMealCardService
{
    private readonly IMealCardRepository _repository;
    private readonly ILogger<MealCardService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MealCardService" /> class.
    /// </summary>
    /// <param name="repository">The repository used to load persisted meal cards.</param>
    /// <param name="logger">The logger for query and validation failures.</param>
    public MealCardService(IMealCardRepository repository, ILogger<MealCardService> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(logger);

        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MealCard>> GetCardsAsync(
        CardSearchCriteria? criteria = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            criteria?.Validate();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Meal card search criteria validation failed.");
            throw;
        }

        var cards = await LoadValidatedCardsAsync(cancellationToken).ConfigureAwait(false);
        if (criteria is null)
        {
            return cards;
        }

        var normalizedKeyword = criteria.GetNormalizedKeyword();
        var filteredCards = cards.Where(
            card =>
                MatchesMealType(card, criteria.MealType)
                && MatchesKeyword(card, normalizedKeyword));

        return filteredCards.ToArray();
    }

    /// <inheritdoc />
    public async Task<MealCard?> GetCardByIdAsync(string cardId, CancellationToken cancellationToken = default)
    {
        string normalizedCardId;
        try
        {
            normalizedCardId = NormalizeCardId(cardId);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Meal card ID validation failed.");
            throw;
        }

        var cards = await LoadValidatedCardsAsync(cancellationToken).ConfigureAwait(false);

        return cards.FirstOrDefault(card => string.Equals(card.Id, normalizedCardId, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<IReadOnlyList<MealCard>> LoadValidatedCardsAsync(CancellationToken cancellationToken)
    {
        var document = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            ValidateLibrary(document.Cards);
        }
        catch (InvalidDataException ex)
        {
            _logger.LogError(ex, "The loaded meal card library failed service-level validation.");
            throw;
        }

        return document.Cards;
    }

    private static void ValidateLibrary(IReadOnlyList<MealCard> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);

        var validationClock = GetValidationClock(cards);
        var contentKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var card in cards)
        {
            ArgumentNullException.ThrowIfNull(card);

            try
            {
                card.Validate(validationClock);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                throw new InvalidDataException($"Meal card '{card.Id}' failed validation.", ex);
            }

            if (!contentKeys.Add(CreateCardContentKey(card)))
            {
                throw new InvalidDataException(
                    $"Duplicate meal card content was found for ID '{card.Id}'.");
            }
        }
    }

    private static bool MatchesMealType(MealCard card, MealType? mealType) =>
        !mealType.HasValue || card.MealType == mealType.Value;

    private static bool MatchesKeyword(MealCard card, string? normalizedKeyword) =>
        normalizedKeyword is null
        || card.Name.Contains(normalizedKeyword, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeCardId(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            throw new ArgumentException("A meal card ID is required.", nameof(cardId));
        }

        if (!Guid.TryParse(cardId.Trim(), out var parsedId))
        {
            throw new ArgumentException("The meal card ID must be a valid GUID value.", nameof(cardId));
        }

        return parsedId.ToString("N");
    }

    private static string CreateCardContentKey(MealCard card) =>
        $"{MealCard.NormalizeName(card.Name)}\u001f{card.MealType}\u001f{MealCard.NormalizeDescription(card.Description)}";

    private static DateTimeOffset GetValidationClock(IEnumerable<MealCard> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);

        var validationClock = DateTimeOffset.UtcNow;
        foreach (var card in cards)
        {
            ArgumentNullException.ThrowIfNull(card);

            if (card.CreatedAtUtc > validationClock)
            {
                validationClock = card.CreatedAtUtc;
            }

            if (card.UpdatedAtUtc > validationClock)
            {
                validationClock = card.UpdatedAtUtc;
            }
        }

        return validationClock;
    }
}
