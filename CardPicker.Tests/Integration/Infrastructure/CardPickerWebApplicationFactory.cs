using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace CardPicker.Tests.Integration.Infrastructure;

/// <summary>
/// Provides an isolated <see cref="WebApplicationFactory{TEntryPoint}" /> for integration tests.
/// </summary>
/// <remarks>
/// The factory redirects the application's content root and card storage settings to a disposable
/// test directory so repository and page tests never modify the real <c>CardPicker/data/cards.json</c>.
/// </remarks>
public sealed class CardPickerWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly string s_applicationRoot = ResolveApplicationRoot();

    /// <summary>
    /// Initializes a new instance of the <see cref="CardPickerWebApplicationFactory" /> class.
    /// </summary>
    public CardPickerWebApplicationFactory()
        : this(TestCardDataDirectory.CreateEmpty())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CardPickerWebApplicationFactory" /> class
    /// with a caller-provided test data directory.
    /// </summary>
    /// <param name="cardDataDirectory">The disposable data directory used by the test host.</param>
    public CardPickerWebApplicationFactory(TestCardDataDirectory cardDataDirectory)
    {
        ArgumentNullException.ThrowIfNull(cardDataDirectory);

        CardDataDirectory = cardDataDirectory;
        PrepareContentRoot(CardDataDirectory.RootPath);
    }

    /// <summary>
    /// Gets the isolated data directory backing the test host.
    /// </summary>
    public TestCardDataDirectory CardDataDirectory { get; }

    /// <summary>
    /// Creates a factory backed by a seeded <c>cards.json</c> file.
    /// </summary>
    /// <param name="cardsJson">The JSON document to place in the isolated card data file.</param>
    /// <returns>A configured factory that uses the seeded test data.</returns>
    public static CardPickerWebApplicationFactory CreateWithCardsJson(string cardsJson)
    {
        ArgumentNullException.ThrowIfNull(cardsJson);

        return new CardPickerWebApplicationFactory(TestCardDataDirectory.CreateWithCardsJson(cardsJson));
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testing");
        builder.UseContentRoot(CardDataDirectory.RootPath);
        builder.ConfigureAppConfiguration(
            (_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["CardStorage:DataDirectoryPath"] = CardDataDirectory.DataDirectoryPath,
                        ["CardStorage:DirectoryPath"] = CardDataDirectory.DataDirectoryPath,
                        ["CardStorage:CardsFilePath"] = CardDataDirectory.CardsFilePath,
                        ["CardStorage:FilePath"] = CardDataDirectory.CardsFilePath,
                        ["CardStorage:FileName"] = "cards.json",
                        ["CardStorageOptions:DataDirectoryPath"] = CardDataDirectory.DataDirectoryPath,
                        ["CardStorageOptions:DirectoryPath"] = CardDataDirectory.DataDirectoryPath,
                        ["CardStorageOptions:CardsFilePath"] = CardDataDirectory.CardsFilePath,
                        ["CardStorageOptions:FilePath"] = CardDataDirectory.CardsFilePath,
                    });
            });
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            CardDataDirectory.Dispose();
        }
    }

    private static void PrepareContentRoot(string contentRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentRootPath);

        Directory.CreateDirectory(contentRootPath);
        Directory.CreateDirectory(Path.Combine(contentRootPath, "logs"));

        CopyIfExists(
            Path.Combine(s_applicationRoot, "appsettings.json"),
            Path.Combine(contentRootPath, "appsettings.json"));

        CopyIfExists(
            Path.Combine(s_applicationRoot, "appsettings.Development.json"),
            Path.Combine(contentRootPath, "appsettings.Development.json"));

        CopyIfExists(
            Path.Combine(s_applicationRoot, "appsettings.Testing.json"),
            Path.Combine(contentRootPath, "appsettings.Testing.json"));
    }

    private static void CopyIfExists(string sourcePath, string destinationPath)
    {
        if (File.Exists(sourcePath))
        {
            File.Copy(sourcePath, destinationPath, overwrite: true);
        }
    }

    private static string ResolveApplicationRoot()
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

        while (currentDirectory is not null)
        {
            var solutionPath = Path.Combine(currentDirectory.FullName, "CardPicker.sln");
            if (File.Exists(solutionPath))
            {
                return Path.Combine(currentDirectory.FullName, nameof(CardPicker));
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException("Unable to locate the CardPicker application root for integration tests.");
    }
}
