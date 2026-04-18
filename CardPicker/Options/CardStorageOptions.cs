using CardPicker.Models;

namespace CardPicker.Options;

/// <summary>
/// Configures where the local meal card JSON document is stored.
/// </summary>
/// <example>
/// <code>
/// builder.Services.Configure&lt;CardStorageOptions&gt;(options =>
/// {
///     options.RelativeFilePath = "data/cards.json";
/// });
/// </code>
/// </example>
public sealed class CardStorageOptions
{
    /// <summary>
    /// Gets the configuration section name.
    /// </summary>
    public const string SectionName = "CardStorage";

    /// <summary>
    /// Gets the default relative JSON file path.
    /// </summary>
    public const string DefaultRelativeFilePath = "data/cards.json";

    /// <summary>
    /// Gets or sets the relative file path under the app content root.
    /// </summary>
    public string RelativeFilePath { get; set; } = DefaultRelativeFilePath;

    /// <summary>
    /// Gets or sets the schema version expected for the persisted root document.
    /// </summary>
    public string SchemaVersion { get; set; } = CardLibraryDocument.CurrentSchemaVersion;

    /// <summary>
    /// Gets or sets a value indicating whether seed data should be created when the JSON file is missing.
    /// </summary>
    public bool CreateSeedDataWhenMissing { get; set; } = true;

    /// <summary>
    /// Returns the normalized relative file path.
    /// </summary>
    /// <returns>The trimmed relative file path using forward slashes.</returns>
    /// <example>
    /// <code>
    /// var filePath = options.GetNormalizedRelativeFilePath();
    /// </code>
    /// </example>
    public string GetNormalizedRelativeFilePath()
    {
        if (string.IsNullOrWhiteSpace(RelativeFilePath))
        {
            throw new InvalidOperationException("RelativeFilePath is required.");
        }

        var normalized = RelativeFilePath.Trim().Replace('\\', '/');
        if (Path.IsPathRooted(normalized))
        {
            throw new InvalidOperationException("RelativeFilePath must stay under the application content root.");
        }

        return normalized;
    }

    /// <summary>
    /// Validates the configured storage settings.
    /// </summary>
    /// <example>
    /// <code>
    /// options.Validate();
    /// </code>
    /// </example>
    public void Validate()
    {
        _ = GetNormalizedRelativeFilePath();

        if (!string.Equals(SchemaVersion, CardLibraryDocument.CurrentSchemaVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"SchemaVersion must be '{CardLibraryDocument.CurrentSchemaVersion}'.");
        }
    }
}
