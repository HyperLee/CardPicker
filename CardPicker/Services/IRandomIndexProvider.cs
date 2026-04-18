namespace CardPicker.Services;

/// <summary>
/// Provides random candidate indexes for uniformly selecting an item from a bounded pool.
/// </summary>
/// <example>
/// <code>
/// var index = randomIndexProvider.GetIndex(3);
/// </code>
/// </example>
public interface IRandomIndexProvider
{
    /// <summary>
    /// Returns a random zero-based index less than the supplied exclusive upper bound.
    /// </summary>
    /// <param name="exclusiveUpperBound">The number of available candidates.</param>
    /// <returns>A random index in the range [0, <paramref name="exclusiveUpperBound" />).</returns>
    /// <example>
    /// <code>
    /// var index = randomIndexProvider.GetIndex(5);
    /// </code>
    /// </example>
    int GetIndex(int exclusiveUpperBound);
}
