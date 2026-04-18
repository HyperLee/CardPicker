using System.Text;

namespace CardPicker.Tests.Integration.Infrastructure;

/// <summary>
/// Represents an isolated content root for integration tests that need a disposable
/// <c>data/cards.json</c> location.
/// </summary>
/// <remarks>
/// The directory structure matches the application convention so tests can exercise
/// file-based storage without touching the repository's real data file.
/// </remarks>
public sealed class TestCardDataDirectory : IDisposable
{
    private const string DataDirectoryName = "data";
    private const string CardsFileName = "cards.json";

    private bool _disposed;

    private TestCardDataDirectory(string rootPath)
    {
        RootPath = rootPath;
        DataDirectoryPath = Path.Combine(rootPath, DataDirectoryName);
        CardsFilePath = Path.Combine(DataDirectoryPath, CardsFileName);

        Directory.CreateDirectory(DataDirectoryPath);
    }

    /// <summary>
    /// Gets the isolated content root used by the test host.
    /// </summary>
    public string RootPath { get; }

    /// <summary>
    /// Gets the path to the isolated <c>data</c> directory.
    /// </summary>
    public string DataDirectoryPath { get; }

    /// <summary>
    /// Gets the path to the isolated <c>data/cards.json</c> file.
    /// </summary>
    public string CardsFilePath { get; }

    /// <summary>
    /// Creates an isolated data directory without a <c>cards.json</c> file.
    /// </summary>
    /// <returns>A disposable directory prepared for integration tests.</returns>
    public static TestCardDataDirectory CreateEmpty()
    {
        var rootPath = Path.Combine(
            Path.GetTempPath(),
            nameof(CardPicker),
            nameof(CardPicker.Tests),
            Guid.CreateVersion7().ToString("N"));

        Directory.CreateDirectory(rootPath);

        return new TestCardDataDirectory(rootPath);
    }

    /// <summary>
    /// Creates an isolated data directory with a seeded <c>cards.json</c> file.
    /// </summary>
    /// <param name="cardsJson">The JSON document to write into <c>cards.json</c>.</param>
    /// <returns>A disposable directory prepared for integration tests.</returns>
    public static TestCardDataDirectory CreateWithCardsJson(string cardsJson)
    {
        ArgumentNullException.ThrowIfNull(cardsJson);

        var directory = CreateEmpty();
        directory.WriteCardsJson(cardsJson);
        return directory;
    }

    /// <summary>
    /// Writes the provided JSON document to the isolated <c>cards.json</c> file.
    /// </summary>
    /// <param name="cardsJson">The JSON document to persist.</param>
    public void WriteCardsJson(string cardsJson)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(cardsJson);

        Directory.CreateDirectory(DataDirectoryPath);
        File.WriteAllText(CardsFilePath, cardsJson, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    /// <summary>
    /// Deletes the isolated <c>cards.json</c> file if it exists.
    /// </summary>
    public void DeleteCardsFile()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (File.Exists(CardsFilePath))
        {
            File.Delete(CardsFilePath);
        }
    }

    /// <summary>
    /// Reads the current JSON document from the isolated <c>cards.json</c> file.
    /// </summary>
    /// <returns>The raw JSON document stored in <c>cards.json</c>.</returns>
    public string ReadCardsJson()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return File.ReadAllText(CardsFilePath, Encoding.UTF8);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Directory.Delete(RootPath, recursive: true);
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
