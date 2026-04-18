namespace CardPicker.Models;

/// <summary>
/// Represents the supported meal periods for meal cards and draw requests.
/// </summary>
/// <example>
/// <code>
/// var mealType = MealType.Lunch;
/// </code>
/// </example>
public enum MealType
{
    /// <summary>
    /// A breakfast meal card.
    /// </summary>
    Breakfast,

    /// <summary>
    /// A lunch meal card.
    /// </summary>
    Lunch,

    /// <summary>
    /// A dinner meal card.
    /// </summary>
    Dinner,
}
