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
    /// <inheritdoc />
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
