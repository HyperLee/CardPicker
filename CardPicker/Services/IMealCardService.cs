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
}
