namespace CardPicker.Models;

/// <summary>
/// Represents the optional filters used to narrow the meal card library.
/// </summary>
/// <example>
/// <code>
/// var criteria = new CardSearchCriteria
/// {
///     Keyword = "便當",
///     MealType = MealType.Lunch,
/// };
/// </code>
/// </example>
public sealed record CardSearchCriteria
{
    /// <summary>
    /// Gets the maximum supported keyword length after trimming.
    /// </summary>
    public const int MaxKeywordLength = MealCard.MaxNameLength;

    /// <summary>
    /// Gets the optional case-insensitive name keyword filter.
    /// </summary>
    public string? Keyword { get; init; }

    /// <summary>
    /// Gets the optional meal type filter.
    /// </summary>
    public MealType? MealType { get; init; }

    /// <summary>
    /// Gets a value indicating whether any search filter has been supplied.
    /// </summary>
    public bool HasFilters => GetNormalizedKeyword() is not null || MealType.HasValue;

    /// <summary>
    /// Returns the trimmed keyword value, or <see langword="null"/> when no keyword was supplied.
    /// </summary>
    /// <returns>The normalized keyword.</returns>
    /// <example>
    /// <code>
    /// var keyword = criteria.GetNormalizedKeyword();
    /// </code>
    /// </example>
    public string? GetNormalizedKeyword()
    {
        if (Keyword is null)
        {
            return null;
        }

        var normalized = Keyword.Trim();
        if (normalized.Length == 0)
        {
            return null;
        }

        if (normalized.Length > MaxKeywordLength)
        {
            throw new ArgumentOutOfRangeException(nameof(Keyword), normalized.Length, $"The keyword cannot exceed {MaxKeywordLength} characters.");
        }

        return normalized;
    }

    /// <summary>
    /// Validates the supplied search criteria.
    /// </summary>
    /// <example>
    /// <code>
    /// criteria.Validate();
    /// </code>
    /// </example>
    public void Validate()
    {
        _ = GetNormalizedKeyword();

        if (MealType.HasValue && !Enum.IsDefined(MealType.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(MealType), MealType, "MealType must be Breakfast, Lunch, or Dinner.");
        }
    }
}
