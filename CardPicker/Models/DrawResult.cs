namespace CardPicker.Models;

/// <summary>
/// Represents the transient result of attempting to draw a meal card.
/// </summary>
/// <example>
/// <code>
/// var result = new DrawResult
/// {
///     State = DrawResultState.Drawn,
///     CardId = "0195f2f63c897b2491c88d6e5f4a3210",
///     CardName = "日式咖哩飯",
///     MealType = MealType.Lunch,
///     Description = "辛香溫和、份量穩定，適合日常午餐。",
/// };
/// </code>
/// </example>
public sealed record DrawResult
{
    /// <summary>
    /// Gets the state of the draw attempt.
    /// </summary>
    public DrawResultState State { get; init; } = DrawResultState.NotRequested;

    /// <summary>
    /// Gets the identifier of the drawn card, or <see langword="null" /> when no card was drawn.
    /// </summary>
    public string? CardId { get; init; }

    /// <summary>
    /// Gets the drawn card name, or <see langword="null" /> when no card was drawn.
    /// </summary>
    public string? CardName { get; init; }

    /// <summary>
    /// Gets the meal type for the drawn card, or <see langword="null" /> when no card was drawn.
    /// </summary>
    public MealType? MealType { get; init; }

    /// <summary>
    /// Gets the drawn card description, or <see langword="null" /> when no card was drawn.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the zh-TW user-facing message for the current state, or <see langword="null" /> when none is needed.
    /// </summary>
    public string? Message { get; init; }
}
