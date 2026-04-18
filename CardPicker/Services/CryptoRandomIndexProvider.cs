using System.Security.Cryptography;

namespace CardPicker.Services;

/// <summary>
/// Provides cryptographically strong random candidate indexes for production draw requests.
/// </summary>
/// <example>
/// <code>
/// var provider = new CryptoRandomIndexProvider();
/// var index = provider.GetIndex(4);
/// </code>
/// </example>
public sealed class CryptoRandomIndexProvider : IRandomIndexProvider
{
    /// <summary>
    /// Returns a cryptographically strong random zero-based index less than the supplied exclusive upper bound.
    /// </summary>
    /// <param name="exclusiveUpperBound">The number of available candidates.</param>
    /// <returns>A uniformly distributed random index in the range [0, <paramref name="exclusiveUpperBound" />).</returns>
    /// <example>
    /// <code>
    /// var index = randomIndexProvider.GetIndex(5);
    /// </code>
    /// </example>
    public int GetIndex(int exclusiveUpperBound)
    {
        if (exclusiveUpperBound <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(exclusiveUpperBound),
                exclusiveUpperBound,
                "The exclusive upper bound must be greater than zero.");
        }

        return RandomNumberGenerator.GetInt32(exclusiveUpperBound);
    }
}
