using CardPicker.Models;

namespace CardPicker.Services;

/// <summary>
/// Provides read-oriented access to the meal card library and shared library validation rules.
/// </summary>
/// <example>
/// <code>
/// var cards = await mealCardService.GetCardsAsync(
///     new CardSearchCriteria { Keyword = "便當", MealType = MealType.Dinner },
///     cancellationToken);
/// </code>
/// </example>
public interface IMealCardService
{
    /// <summary>
    /// Returns meal cards that match the supplied search criteria.
    /// </summary>
    /// <param name="criteria">
    /// Optional filters for the query. When <see langword="null" />, all cards are returned.
    /// </param>
    /// <param name="cancellationToken">The cancellation token for the query.</param>
    /// <returns>A read-only list of cards that satisfy the criteria.</returns>
    /// <example>
    /// <code>
    /// var cards = await mealCardService.GetCardsAsync(
    ///     new CardSearchCriteria { Keyword = "牛肉", MealType = MealType.Lunch },
    ///     cancellationToken);
    /// </code>
    /// </example>
    Task<IReadOnlyList<MealCard>> GetCardsAsync(
        CardSearchCriteria? criteria = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a single meal card by its immutable identifier.
    /// </summary>
    /// <param name="cardId">The meal card identifier.</param>
    /// <param name="cancellationToken">The cancellation token for the query.</param>
    /// <returns>The matching meal card, or <see langword="null" /> when the ID does not exist.</returns>
    /// <example>
    /// <code>
    /// var card = await mealCardService.GetCardByIdAsync(cardId, cancellationToken);
    /// </code>
    /// </example>
    Task<MealCard?> GetCardByIdAsync(string cardId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new meal card with normalized content and persists it to the library.
    /// </summary>
    /// <param name="name">The meal name.</param>
    /// <param name="mealType">The meal period.</param>
    /// <param name="description">The meal description.</param>
    /// <param name="cancellationToken">The cancellation token for the mutation.</param>
    /// <returns>The persisted meal card.</returns>
    /// <example>
    /// <code>
    /// var card = await mealCardService.CreateCardAsync("雞腿便當", MealType.Lunch, "快速方便。", cancellationToken);
    /// </code>
    /// </example>
    Task<MealCard> CreateCardAsync(
        string name,
        MealType mealType,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the mutable content of an existing meal card while preserving immutable fields.
    /// </summary>
    /// <param name="cardId">The immutable meal card identifier.</param>
    /// <param name="name">The replacement meal name.</param>
    /// <param name="mealType">The replacement meal period.</param>
    /// <param name="description">The replacement meal description.</param>
    /// <param name="cancellationToken">The cancellation token for the mutation.</param>
    /// <returns>The persisted updated meal card.</returns>
    /// <example>
    /// <code>
    /// var updated = await mealCardService.EditCardAsync(cardId, "鮭魚便當", MealType.Dinner, "下班後快速解決晚餐。", cancellationToken);
    /// </code>
    /// </example>
    Task<MealCard> EditCardAsync(
        string cardId,
        string name,
        MealType mealType,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a meal card from the persisted library.
    /// </summary>
    /// <param name="cardId">The immutable meal card identifier.</param>
    /// <param name="cancellationToken">The cancellation token for the mutation.</param>
    /// <returns>A task that completes when the card has been deleted.</returns>
    /// <example>
    /// <code>
    /// await mealCardService.DeleteCardAsync(cardId, cancellationToken);
    /// </code>
    /// </example>
    Task DeleteCardAsync(string cardId, CancellationToken cancellationToken = default);
}
