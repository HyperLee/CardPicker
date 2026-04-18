namespace CardPicker.Models;

/// <summary>
/// Represents a single draw request from the home page meal picker form.
/// </summary>
/// <example>
/// <code>
/// var request = new DrawRequest
/// {
///     SelectedMealType = MealType.Dinner,
/// };
/// </code>
/// </example>
public sealed record DrawRequest
{
    /// <summary>
    /// Gets the selected meal type to draw from.
    /// </summary>
    public MealType? SelectedMealType { get; init; }
}
