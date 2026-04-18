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
    private const string DuplicateMealCardMessage = "相同內容的卡牌已存在，請勿重複新增。";

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

    /// <summary>
    /// Returns persisted meal cards that match the supplied criteria after validating the query filters.
    /// </summary>
    /// <param name="criteria">
    /// Optional filters for the query. When <see langword="null" />, all validated cards are returned.
    /// </param>
    /// <param name="cancellationToken">The cancellation token for the query.</param>
    /// <returns>A read-only list of meal cards that satisfy the supplied criteria.</returns>
    /// <example>
    /// <code>
    /// var cards = await mealCardService.GetCardsAsync(
    ///     new CardSearchCriteria { Keyword = "便當", MealType = MealType.Lunch },
    ///     cancellationToken);
    /// </code>
    /// </example>
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

    /// <summary>
    /// Returns one validated meal card that matches the supplied identifier.
    /// </summary>
    /// <param name="cardId">The immutable meal card identifier.</param>
    /// <param name="cancellationToken">The cancellation token for the query.</param>
    /// <returns>The matching meal card, or <see langword="null" /> when the card does not exist.</returns>
    /// <example>
    /// <code>
    /// var card = await mealCardService.GetCardByIdAsync(cardId, cancellationToken);
    /// </code>
    /// </example>
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

    /// <summary>
    /// Creates a new meal card, rejects duplicate content, and persists the updated library document.
    /// </summary>
    /// <param name="name">The meal name.</param>
    /// <param name="mealType">The meal period.</param>
    /// <param name="description">The meal description.</param>
    /// <param name="cancellationToken">The cancellation token for the mutation.</param>
    /// <returns>The newly created persisted meal card.</returns>
    /// <example>
    /// <code>
    /// var card = await mealCardService.CreateCardAsync(
    ///     "雞腿便當",
    ///     MealType.Lunch,
    ///     "快速方便。",
    ///     cancellationToken);
    /// </code>
    /// </example>
    public async Task<MealCard> CreateCardAsync(
        string name,
        MealType mealType,
        string description,
        CancellationToken cancellationToken = default)
    {
        MealCard createdCard;
        try
        {
            createdCard = MealCard.Create(name, ValidateMealType(mealType), description);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Meal card create validation failed.");
            throw;
        }

        var document = await LoadValidatedDocumentAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            EnsureUniqueCardContent(document.Cards, createdCard);
        }
        catch (InvalidDataException ex)
        {
            _logger.LogWarning(ex, "Meal card create validation failed.");
            throw;
        }

        var updatedDocument = document.WithCards([.. document.Cards, createdCard]);
        await SaveDocumentAsync(
                updatedDocument,
                createdCard.Id,
                "created",
                cancellationToken)
            .ConfigureAwait(false);

        return createdCard;
    }

    /// <summary>
    /// Updates an existing meal card, preserving immutable fields while preventing duplicate card content.
    /// </summary>
    /// <param name="cardId">The immutable meal card identifier.</param>
    /// <param name="name">The replacement meal name.</param>
    /// <param name="mealType">The replacement meal period.</param>
    /// <param name="description">The replacement meal description.</param>
    /// <param name="cancellationToken">The cancellation token for the mutation.</param>
    /// <returns>The updated persisted meal card.</returns>
    /// <example>
    /// <code>
    /// var updated = await mealCardService.EditCardAsync(
    ///     cardId,
    ///     "鮭魚便當",
    ///     MealType.Dinner,
    ///     "下班後快速解決晚餐。",
    ///     cancellationToken);
    /// </code>
    /// </example>
    public async Task<MealCard> EditCardAsync(
        string cardId,
        string name,
        MealType mealType,
        string description,
        CancellationToken cancellationToken = default)
    {
        string normalizedCardId;
        try
        {
            normalizedCardId = NormalizeCardId(cardId);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Meal card edit validation failed.");
            throw;
        }

        var document = await LoadValidatedDocumentAsync(cancellationToken).ConfigureAwait(false);
        var existingCard = document.Cards.FirstOrDefault(
            card => string.Equals(card.Id, normalizedCardId, StringComparison.OrdinalIgnoreCase));

        if (existingCard is null)
        {
            var exception = new KeyNotFoundException($"Meal card '{normalizedCardId}' was not found.");
            _logger.LogWarning(exception, "Meal card edit validation failed.");
            throw exception;
        }

        MealCard updatedCard;
        try
        {
            updatedCard = existingCard.WithUpdatedContent(name, ValidateMealType(mealType), description);
            EnsureUniqueCardContent(document.Cards, updatedCard, normalizedCardId);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidDataException)
        {
            _logger.LogWarning(ex, "Meal card edit validation failed.");
            throw;
        }

        var updatedDocument = document.WithCards(
            document.Cards.Select(
                card => string.Equals(card.Id, normalizedCardId, StringComparison.OrdinalIgnoreCase)
                    ? updatedCard
                    : card));

        await SaveDocumentAsync(
                updatedDocument,
                normalizedCardId,
                "updated",
                cancellationToken)
            .ConfigureAwait(false);

        return updatedCard;
    }

    /// <summary>
    /// Deletes one meal card from the persisted library by identifier.
    /// </summary>
    /// <param name="cardId">The immutable meal card identifier.</param>
    /// <param name="cancellationToken">The cancellation token for the mutation.</param>
    /// <returns>A task that completes when the card has been removed and persisted.</returns>
    /// <example>
    /// <code>
    /// await mealCardService.DeleteCardAsync(cardId, cancellationToken);
    /// </code>
    /// </example>
    public async Task DeleteCardAsync(string cardId, CancellationToken cancellationToken = default)
    {
        string normalizedCardId;
        try
        {
            normalizedCardId = NormalizeCardId(cardId);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Meal card delete validation failed.");
            throw;
        }

        var document = await LoadValidatedDocumentAsync(cancellationToken).ConfigureAwait(false);
        var remainingCards = document.Cards
            .Where(card => !string.Equals(card.Id, normalizedCardId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (remainingCards.Length == document.Cards.Count)
        {
            var exception = new KeyNotFoundException($"Meal card '{normalizedCardId}' was not found.");
            _logger.LogWarning(exception, "Meal card delete validation failed.");
            throw exception;
        }

        var updatedDocument = document.WithCards(remainingCards);
        await SaveDocumentAsync(
                updatedDocument,
                normalizedCardId,
                "deleted",
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<MealCard>> LoadValidatedCardsAsync(CancellationToken cancellationToken)
    {
        var document = await LoadValidatedDocumentAsync(cancellationToken).ConfigureAwait(false);
        return document.Cards;
    }

    private async Task<CardLibraryDocument> LoadValidatedDocumentAsync(CancellationToken cancellationToken)
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

        return document;
    }

    private async Task SaveDocumentAsync(
        CardLibraryDocument document,
        string cardId,
        string operationName,
        CancellationToken cancellationToken)
    {
        try
        {
            await _repository.SaveAsync(document, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
        {
            _logger.LogError(ex, "Failed to persist {OperationName} meal card {CardId}.", operationName, cardId);
            throw;
        }
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

    private static void EnsureUniqueCardContent(
        IReadOnlyList<MealCard> existingCards,
        MealCard candidateCard,
        string? excludedCardId = null)
    {
        ArgumentNullException.ThrowIfNull(existingCards);
        ArgumentNullException.ThrowIfNull(candidateCard);

        var candidateKey = CreateCardContentKey(candidateCard);
        foreach (var existingCard in existingCards)
        {
            ArgumentNullException.ThrowIfNull(existingCard);

            if (excludedCardId is not null
                && string.Equals(existingCard.Id, excludedCardId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(CreateCardContentKey(existingCard), candidateKey, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(DuplicateMealCardMessage);
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

    private static MealType ValidateMealType(MealType mealType)
    {
        if (!Enum.IsDefined(mealType))
        {
            throw new ArgumentOutOfRangeException(nameof(mealType), mealType, "Meal type must be Breakfast, Lunch, or Dinner.");
        }

        return mealType;
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
