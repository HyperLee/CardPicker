namespace CardPicker.Models;

/// <summary>
/// Represents the supported states for a meal draw attempt.
/// </summary>
/// <example>
/// <code>
/// var state = DrawResultState.Drawn;
/// </code>
/// </example>
public enum DrawResultState
{
    /// <summary>
    /// No draw has been requested yet.
    /// </summary>
    NotRequested,

    /// <summary>
    /// The request was invalid, such as when no meal type was selected.
    /// </summary>
    ValidationFailed,

    /// <summary>
    /// The selected meal type currently has no candidate cards.
    /// </summary>
    EmptyPool,

    /// <summary>
    /// A meal card was successfully drawn.
    /// </summary>
    Drawn,
}
