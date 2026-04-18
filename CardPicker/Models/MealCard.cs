using System.Text.Json.Serialization;

namespace CardPicker.Models;

/// <summary>
/// Represents a single persisted meal card in the local card library.
/// </summary>
/// <example>
/// <code>
/// var card = MealCard.Create(
///     "紅燒牛肉麵",
///     MealType.Lunch,
///     "湯頭濃郁，適合想吃熱食的中午。");
/// </code>
/// </example>
public sealed record MealCard
{
    /// <summary>
    /// Gets the maximum supported name length after trimming.
    /// </summary>
    public const int MaxNameLength = 120;

    /// <summary>
    /// Gets the maximum supported description length after trimming and newline normalization.
    /// </summary>
    public const int MaxDescriptionLength = 2_000;

    /// <summary>
    /// Gets the immutable unique identifier for the card.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets the normalized meal name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the meal period that the card belongs to.
    /// </summary>
    [JsonPropertyName("mealType")]
    public MealType MealType { get; init; }

    /// <summary>
    /// Gets the normalized meal description.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    [JsonPropertyName("createdAtUtc")]
    public DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>
    /// Gets the UTC timestamp for the latest update.
    /// </summary>
    [JsonPropertyName("updatedAtUtc")]
    public DateTimeOffset UpdatedAtUtc { get; init; }

    /// <summary>
    /// Creates a new card with a GUID v7 identifier and normalized content.
    /// </summary>
    /// <param name="name">The meal name.</param>
    /// <param name="mealType">The meal period.</param>
    /// <param name="description">The meal description.</param>
    /// <param name="timestampUtc">An optional UTC timestamp override.</param>
    /// <returns>A validated meal card instance.</returns>
    /// <example>
    /// <code>
    /// var card = MealCard.Create(
    ///     "鮭魚便當",
    ///     MealType.Dinner,
    ///     "主菜、青菜和白飯一次搞定。");
    /// </code>
    /// </example>
    public static MealCard Create(
        string name,
        MealType mealType,
        string description,
        DateTimeOffset? timestampUtc = null)
    {
        var normalizedTimestamp = NormalizeTimestamp(timestampUtc ?? DateTimeOffset.UtcNow, nameof(timestampUtc));

        var card = new MealCard
        {
            Id = Guid.CreateVersion7().ToString("N"),
            Name = NormalizeName(name),
            MealType = mealType,
            Description = NormalizeDescription(description),
            CreatedAtUtc = normalizedTimestamp,
            UpdatedAtUtc = normalizedTimestamp,
        };

        card.Validate(normalizedTimestamp);
        return card;
    }

    /// <summary>
    /// Returns a copy of the current card with updated normalized content and a refreshed update timestamp.
    /// </summary>
    /// <param name="name">The replacement meal name.</param>
    /// <param name="mealType">The replacement meal period.</param>
    /// <param name="description">The replacement description.</param>
    /// <param name="updatedAtUtc">An optional UTC timestamp override for the update time.</param>
    /// <returns>A validated updated copy that preserves <see cref="Id"/> and <see cref="CreatedAtUtc"/>.</returns>
    /// <example>
    /// <code>
    /// var updated = card.WithUpdatedContent(
    ///     "雞腿便當",
    ///     MealType.Lunch,
    ///     "配菜固定、選擇快速。",
    ///     DateTimeOffset.UtcNow);
    /// </code>
    /// </example>
    public MealCard WithUpdatedContent(
        string name,
        MealType mealType,
        string description,
        DateTimeOffset? updatedAtUtc = null)
    {
        var updatedCard = this with
        {
            Name = NormalizeName(name),
            MealType = mealType,
            Description = NormalizeDescription(description),
            UpdatedAtUtc = NormalizeTimestamp(updatedAtUtc ?? DateTimeOffset.UtcNow, nameof(updatedAtUtc)),
        };

        updatedCard.Validate(updatedCard.UpdatedAtUtc);
        return updatedCard;
    }

    /// <summary>
    /// Validates that the card content and timestamps match the domain rules.
    /// </summary>
    /// <param name="utcNow">An optional UTC clock value used for future-time validation.</param>
    /// <example>
    /// <code>
    /// card.Validate(DateTimeOffset.UtcNow);
    /// </code>
    /// </example>
    public void Validate(DateTimeOffset? utcNow = null)
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new InvalidOperationException("Meal card ID is required.");
        }

        if (!Guid.TryParse(Id, out _))
        {
            throw new InvalidOperationException("Meal card ID must be a valid GUID value.");
        }

        _ = NormalizeName(Name);
        _ = NormalizeDescription(Description);

        if (!Enum.IsDefined(MealType))
        {
            throw new InvalidOperationException("Meal type must be Breakfast, Lunch, or Dinner.");
        }

        if (CreatedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new InvalidOperationException("CreatedAtUtc must use a UTC offset.");
        }

        if (UpdatedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new InvalidOperationException("UpdatedAtUtc must use a UTC offset.");
        }

        if (UpdatedAtUtc < CreatedAtUtc)
        {
            throw new InvalidOperationException("UpdatedAtUtc must be greater than or equal to CreatedAtUtc.");
        }

        var normalizedNow = NormalizeTimestamp(utcNow ?? DateTimeOffset.UtcNow, nameof(utcNow));
        if (CreatedAtUtc > normalizedNow)
        {
            throw new InvalidOperationException("CreatedAtUtc cannot be in the future.");
        }

        if (UpdatedAtUtc > normalizedNow)
        {
            throw new InvalidOperationException("UpdatedAtUtc cannot be in the future.");
        }
    }

    /// <summary>
    /// Normalizes and validates a meal name.
    /// </summary>
    /// <param name="name">The incoming meal name.</param>
    /// <returns>The trimmed meal name.</returns>
    /// <example>
    /// <code>
    /// var normalizedName = MealCard.NormalizeName("  火腿蛋吐司  ");
    /// </code>
    /// </example>
    public static string NormalizeName(string name) => NormalizeText(name, nameof(name), MaxNameLength);

    /// <summary>
    /// Normalizes and validates a meal description.
    /// </summary>
    /// <param name="description">The incoming description.</param>
    /// <returns>The trimmed description with normalized newlines.</returns>
    /// <example>
    /// <code>
    /// var normalizedDescription = MealCard.NormalizeDescription("  推薦加蛋\r\n外帶很快  ");
    /// </code>
    /// </example>
    public static string NormalizeDescription(string description) => NormalizeText(description, nameof(description), MaxDescriptionLength);

    private static DateTimeOffset NormalizeTimestamp(DateTimeOffset timestamp, string paramName)
    {
        if (string.IsNullOrWhiteSpace(paramName))
        {
            throw new ArgumentException("Parameter name is required.", nameof(paramName));
        }

        return timestamp.ToUniversalTime();
    }

    private static string NormalizeText(string value, string paramName, int maxLength)
    {
        ArgumentNullException.ThrowIfNull(value);

        var normalized = NormalizeLineEndings(value).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("A non-empty value is required.", paramName);
        }

        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, normalized.Length, $"The value cannot exceed {maxLength} characters.");
        }

        return normalized;
    }

    private static string NormalizeLineEndings(string value) =>
        value
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal);
}
