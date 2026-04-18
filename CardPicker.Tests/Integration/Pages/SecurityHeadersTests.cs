using System.Text.Json;
using CardPicker.Models;
using CardPicker.Tests.Integration.Infrastructure;

namespace CardPicker.Tests.Integration.Pages;

public sealed class SecurityHeadersTests
{
    private const string ExistingCardId = "0195f2f4e7d47c6496ef0bbca4e6df6d";

    [Theory]
    [InlineData("/")]
    [InlineData("/Cards")]
    [InlineData("/Cards/Create")]
    [InlineData($"/Cards/Edit?id={ExistingCardId}")]
    [InlineData($"/Cards/Delete?id={ExistingCardId}")]
    public async Task GetAsync_WhenRunningInProductionLikeEnvironment_EmitsRequiredSecurityHeaders(string path)
    {
        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(
            CreateCardsJson(
                CreateCard(
                    ExistingCardId,
                    "火腿蛋吐司",
                    MealType.Breakfast,
                    "附近早餐店的招牌組合，五分鐘內可以外帶。")));
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
            });

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "default-src 'self'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'; img-src 'self' data:; object-src 'none'; script-src 'self' 'unsafe-inline'; style-src 'self'; upgrade-insecure-requests",
            Assert.Single(response.Headers.GetValues("Content-Security-Policy")));
        Assert.Equal("strict-origin-when-cross-origin", Assert.Single(response.Headers.GetValues("Referrer-Policy")));
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("DENY", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
        Assert.Equal("camera=(), geolocation=(), microphone=()", Assert.Single(response.Headers.GetValues("Permissions-Policy")));

        var hstsHeader = Assert.Single(response.Headers.GetValues("Strict-Transport-Security"));
        Assert.Contains("max-age=", hstsHeader, StringComparison.Ordinal);
        Assert.Contains("includeSubDomains", hstsHeader, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateCardsJson(params object[] cards)
    {
        ArgumentNullException.ThrowIfNull(cards);

        return JsonSerializer.Serialize(
            new
            {
                schemaVersion = CardLibraryDocument.CurrentSchemaVersion,
                cards,
            });
    }

    private static object CreateCard(string id, string name, MealType mealType, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(description);

        var timestamp = new DateTimeOffset(2024, 1, 1, 8, 0, 0, TimeSpan.Zero);
        return new
        {
            id,
            name,
            mealType = mealType.ToString(),
            description,
            createdAtUtc = timestamp,
            updatedAtUtc = timestamp,
        };
    }

}
