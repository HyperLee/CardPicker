using System.Text.Json.Serialization;

namespace CardPicker.Models;

/// <summary>
/// Represents the versioned root document stored in <c>cards.json</c>.
/// </summary>
/// <example>
/// <code>
/// var document = CardLibraryDocument.CreateDefault(
/// [
///     MealCard.Create("火腿蛋吐司", MealType.Breakfast, "五分鐘內可外帶。"),
/// ]);
/// </code>
/// </example>
public sealed record CardLibraryDocument
{
    /// <summary>
    /// Gets the current JSON schema version.
    /// </summary>
    public const string CurrentSchemaVersion = "1.0";

    /// <summary>
    /// Gets the schema version persisted to disk.
    /// </summary>
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>
    /// Gets the persisted meal card collection.
    /// </summary>
    [JsonPropertyName("cards")]
    public IReadOnlyList<MealCard> Cards { get; init; } = [];

    /// <summary>
    /// Creates a default schema document from the provided cards.
    /// </summary>
    /// <param name="cards">The cards to include in the document.</param>
    /// <returns>A validated root document using schema version <c>1.0</c>.</returns>
    /// <example>
    /// <code>
    /// var document = CardLibraryDocument.CreateDefault(cards);
    /// </code>
    /// </example>
    public static CardLibraryDocument CreateDefault(IEnumerable<MealCard>? cards = null)
    {
        var document = new CardLibraryDocument
        {
            Cards = cards?.ToArray() ?? [],
        };

        document.Validate();
        return document;
    }

    /// <summary>
    /// Returns a copy of the document with a replacement card list.
    /// </summary>
    /// <param name="cards">The replacement cards.</param>
    /// <returns>A validated copy of the document.</returns>
    /// <example>
    /// <code>
    /// var updated = document.WithCards(cards);
    /// </code>
    /// </example>
    public CardLibraryDocument WithCards(IEnumerable<MealCard> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);

        var updated = this with
        {
            Cards = cards.ToArray(),
        };

        updated.Validate();
        return updated;
    }

    /// <summary>
    /// Validates schema compatibility and unique card identifiers.
    /// </summary>
    /// <param name="utcNow">An optional UTC clock value used for card timestamp validation.</param>
    /// <example>
    /// <code>
    /// document.Validate(DateTimeOffset.UtcNow);
    /// </code>
    /// </example>
    public void Validate(DateTimeOffset? utcNow = null)
    {
        if (!string.Equals(SchemaVersion, CurrentSchemaVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unsupported schema version '{SchemaVersion}'.");
        }

        ArgumentNullException.ThrowIfNull(Cards);

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var card in Cards)
        {
            ArgumentNullException.ThrowIfNull(card);
            card.Validate(utcNow);

            if (!ids.Add(card.Id))
            {
                throw new InvalidOperationException($"Duplicate meal card ID '{card.Id}' was found in the document.");
            }
        }
    }
}
