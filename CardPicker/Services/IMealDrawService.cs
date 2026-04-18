using CardPicker.Models;

namespace CardPicker.Services;

/// <summary>
/// Draws a random meal card from the selected meal type pool.
/// </summary>
/// <example>
/// <code>
/// var result = await mealDrawService.DrawAsync(
///     new DrawRequest { SelectedMealType = MealType.Dinner },
///     cancellationToken);
/// </code>
/// </example>
public interface IMealDrawService
{
    /// <summary>
    /// Attempts to draw one meal card for the requested meal type.
    /// </summary>
    /// <param name="request">The draw request from the home page.</param>
    /// <param name="cancellationToken">The cancellation token for the draw operation.</param>
    /// <returns>
    /// A draw result describing whether validation failed, the pool was empty, or a card was drawn.
    /// </returns>
    /// <example>
    /// <code>
    /// var result = await mealDrawService.DrawAsync(
    ///     new DrawRequest { SelectedMealType = MealType.Breakfast },
    ///     cancellationToken);
    /// </code>
    /// </example>
    Task<DrawResult> DrawAsync(DrawRequest request, CancellationToken cancellationToken = default);
}
