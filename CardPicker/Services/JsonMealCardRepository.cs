using System.Text.Json;
using System.Text.Json.Serialization;
using CardPicker.Models;
using CardPicker.Options;
using Microsoft.Extensions.Options;

namespace CardPicker.Services;

/// <summary>
/// Stores the meal card library in a single versioned JSON file under the application content root.
/// </summary>
/// <example>
/// <code>
/// var document = await repository.LoadAsync(cancellationToken);
/// await repository.SaveAsync(document, cancellationToken);
/// </code>
/// </example>
public sealed class JsonMealCardRepository : IMealCardRepository
{
    private static readonly JsonSerializerOptions s_serializerOptions = CreateSerializerOptions();

    private readonly ILogger<JsonMealCardRepository> _logger;
    private readonly string _cardFilePath;
    private readonly string _expectedSchemaVersion;
    private readonly bool _createSeedDataWhenMissing;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonMealCardRepository" /> class.
    /// </summary>
    /// <param name="options">The configured card storage options.</param>
    /// <param name="hostEnvironment">The host environment used to resolve the content root path.</param>
    /// <param name="logger">The logger for repository operations.</param>
    public JsonMealCardRepository(
        IOptions<CardStorageOptions> options,
        IHostEnvironment hostEnvironment,
        ILogger<JsonMealCardRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(hostEnvironment);
        ArgumentNullException.ThrowIfNull(logger);

        var storageOptions = options.Value;
        storageOptions.Validate();

        _logger = logger;
        _expectedSchemaVersion = storageOptions.SchemaVersion;
        _createSeedDataWhenMissing = storageOptions.CreateSeedDataWhenMissing;
        _cardFilePath = ResolveCardFilePath(hostEnvironment.ContentRootPath, storageOptions);
    }

    /// <inheritdoc />
    public async Task<CardLibraryDocument> LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(_cardFilePath))
        {
            if (!_createSeedDataWhenMissing)
            {
                throw new FileNotFoundException("The meal card library file does not exist.", _cardFilePath);
            }

            await InitializeStorageAsync(cancellationToken).ConfigureAwait(false);
        }

        return await ReadDocumentAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task SaveAsync(CardLibraryDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        return PersistDocumentAsync(document, isInitialization: false, cancellationToken);
    }

    private async Task<CardLibraryDocument> ReadDocumentAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = new FileStream(
                _cardFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 16 * 1024,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);

            var document = await JsonSerializer
                .DeserializeAsync<CardLibraryDocument>(stream, s_serializerOptions, cancellationToken)
                .ConfigureAwait(false);

            if (document is null)
            {
                throw new InvalidDataException($"The meal card library file '{_cardFilePath}' did not contain a valid document.");
            }

            ValidateDocument(document);
            return document;
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Failed to load meal card library from {CardLibraryPath}.", _cardFilePath);
            throw;
        }
    }

    private async Task InitializeStorageAsync(CancellationToken cancellationToken)
    {
        var seedDocument = CreateSeedDocument();
        await PersistDocumentAsync(seedDocument, isInitialization: true, cancellationToken).ConfigureAwait(false);
    }

    private async Task PersistDocumentAsync(
        CardLibraryDocument document,
        bool isInitialization,
        CancellationToken cancellationToken)
    {
        ValidateDocument(document);

        var directoryPath = Path.GetDirectoryName(_cardFilePath)
            ?? throw new InvalidOperationException("The meal card library path must include a directory.");
        Directory.CreateDirectory(directoryPath);

        var tempFilePath = Path.Combine(
            directoryPath,
            $"{Path.GetFileName(_cardFilePath)}.{Guid.CreateVersion7():N}.tmp");

        try
        {
            await using (var stream = new FileStream(
                tempFilePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 16 * 1024,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await JsonSerializer
                    .SerializeAsync(stream, document, s_serializerOptions, cancellationToken)
                    .ConfigureAwait(false);

                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            File.Move(tempFilePath, _cardFilePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException or UnauthorizedAccessException)
        {
            TryDeleteTempFile(tempFilePath);

            if (isInitialization)
            {
                _logger.LogError(ex, "Failed to initialize meal card library at {CardLibraryPath}.", _cardFilePath);
            }
            else
            {
                _logger.LogError(ex, "Failed to persist meal card library to {CardLibraryPath}.", _cardFilePath);
            }

            throw;
        }
    }

    private void ValidateDocument(CardLibraryDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        try
        {
            if (!string.Equals(document.SchemaVersion, _expectedSchemaVersion, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Expected schema version '{_expectedSchemaVersion}', but found '{document.SchemaVersion}'.");
            }

            var validationClock = GetValidationClock(document.Cards);
            document.Validate(validationClock);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            throw new InvalidDataException("The meal card library document failed validation.", ex);
        }
    }

    private static string ResolveCardFilePath(string contentRootPath, CardStorageOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentRootPath);
        ArgumentNullException.ThrowIfNull(options);

        var contentRootFullPath = EnsureTrailingDirectorySeparator(Path.GetFullPath(contentRootPath));
        var combinedPath = Path.GetFullPath(Path.Combine(contentRootFullPath, options.GetNormalizedRelativeFilePath()));

        if (!combinedPath.StartsWith(contentRootFullPath, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Card storage path must stay under the application content root.");
        }

        return combinedPath;
    }

    private static string EnsureTrailingDirectorySeparator(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return path[^1] == Path.DirectorySeparatorChar || path[^1] == Path.AltDirectorySeparatorChar
            ? path
            : path + Path.DirectorySeparatorChar;
    }

    private static DateTimeOffset GetValidationClock(IEnumerable<MealCard> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);

        var validationClock = DateTimeOffset.UtcNow;
        foreach (var card in cards)
        {
            ArgumentNullException.ThrowIfNull(card);

            if (card.CreatedAtUtc > validationClock)
            {
                validationClock = card.CreatedAtUtc;
            }

            if (card.UpdatedAtUtc > validationClock)
            {
                validationClock = card.UpdatedAtUtc;
            }
        }

        return validationClock;
    }

    private static CardLibraryDocument CreateSeedDocument() =>
        CardLibraryDocument.CreateDefault(
        [
            CreateSeedCard(
                "0195f2f4e7d47c6496ef0bbca4e6df6d",
                "火腿蛋吐司",
                MealType.Breakfast,
                "附近早餐店的招牌組合，五分鐘內可以外帶。",
                new DateTimeOffset(2024, 1, 1, 8, 0, 0, TimeSpan.Zero)),
            CreateSeedCard(
                "0195f2f5a1b67d1f9a2d4c9e1f0a2b3c",
                "紅燒牛肉麵",
                MealType.Lunch,
                "湯頭濃郁，適合想吃熱食又需要飽足感的中午。",
                new DateTimeOffset(2024, 1, 1, 8, 5, 0, TimeSpan.Zero)),
            CreateSeedCard(
                "0195f2f63c897b2491c88d6e5f4a3210",
                "鮭魚便當",
                MealType.Dinner,
                "有主菜、青菜和白飯，適合下班後快速解決晚餐。",
                new DateTimeOffset(2024, 1, 1, 8, 10, 0, TimeSpan.Zero)),
        ]);

    private static MealCard CreateSeedCard(
        string id,
        string name,
        MealType mealType,
        string description,
        DateTimeOffset timestampUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        return new MealCard
        {
            Id = id.Trim(),
            Name = MealCard.NormalizeName(name),
            MealType = mealType,
            Description = MealCard.NormalizeDescription(description),
            CreatedAtUtc = timestampUtc.ToUniversalTime(),
            UpdatedAtUtc = timestampUtc.ToUniversalTime(),
        };
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = false,
            ReadCommentHandling = JsonCommentHandling.Disallow,
            AllowTrailingCommas = false,
            WriteIndented = true,
        };

        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private void TryDeleteTempFile(string tempFilePath)
    {
        if (!File.Exists(tempFilePath))
        {
            return;
        }

        try
        {
            File.Delete(tempFilePath);
        }
        catch (IOException)
        {
            _logger.LogWarning("Failed to delete temporary meal card file {TemporaryCardLibraryPath}.", tempFilePath);
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Failed to delete temporary meal card file {TemporaryCardLibraryPath}.", tempFilePath);
        }
    }
}
