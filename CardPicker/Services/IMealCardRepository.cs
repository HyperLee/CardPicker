using CardPicker.Models;

namespace CardPicker.Services;

/// <summary>
/// Provides versioned JSON persistence for the local meal card library.
/// </summary>
/// <example>
/// <code>
/// var document = await repository.LoadAsync(cancellationToken);
/// await repository.SaveAsync(document, cancellationToken);
/// </code>
/// </example>
public interface IMealCardRepository
{
    /// <summary>
    /// Loads the persisted meal card library document from storage.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the load operation.</param>
    /// <returns>
    /// A validated <see cref="CardLibraryDocument" />. When storage is missing and seeding is enabled,
    /// the repository initializes the file before returning the document.
    /// </returns>
    /// <example>
    /// <code>
    /// var document = await repository.LoadAsync(cancellationToken);
    /// var cards = document.Cards;
    /// </code>
    /// </example>
    Task<CardLibraryDocument> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the supplied meal card library document by atomically replacing the JSON file.
    /// </summary>
    /// <param name="document">The replacement document to persist.</param>
    /// <param name="cancellationToken">The cancellation token for the save operation.</param>
    /// <returns>A task that completes when the document has been written.</returns>
    /// <example>
    /// <code>
    /// var document = await repository.LoadAsync(cancellationToken);
    /// var updated = document.WithCards(document.Cards);
    /// await repository.SaveAsync(updated, cancellationToken);
    /// </code>
    /// </example>
    Task SaveAsync(CardLibraryDocument document, CancellationToken cancellationToken = default);
}
