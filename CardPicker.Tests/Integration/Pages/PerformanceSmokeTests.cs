using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using CardPicker.Models;
using CardPicker.Tests.Integration.Infrastructure;

namespace CardPicker.Tests.Integration.Pages;

public sealed class PerformanceSmokeTests
{
    private static readonly TimeSpan s_smokeP95Budget = TimeSpan.FromMilliseconds(750);
    private const long SingleRequestAllocationBudgetBytes = 100L * 1024L * 1024L;

    [Fact]
    public async Task DrawAndSearchFlows_WhenWarmedUp_StayWithinSmokeResponseBudgets()
    {
        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(CreateCardsJson(CreateCards(180)));
        using var client = CreateHttpsClient(factory);

        var antiForgeryToken = await GetAntiForgeryTokenAsync(client, "/");

        await ExecuteDrawAsync(client, antiForgeryToken, MealType.Dinner);
        await ExecuteSearchAsync(client, "/Cards?keyword=Meal&mealType=Dinner");

        var drawSamples = new List<TimeSpan>();
        var searchSamples = new List<TimeSpan>();

        for (var index = 0; index < 20; index++)
        {
            drawSamples.Add(await MeasureAsync(() => ExecuteDrawAsync(client, antiForgeryToken, MealType.Dinner)));
            searchSamples.Add(await MeasureAsync(() => ExecuteSearchAsync(client, "/Cards?keyword=Meal&mealType=Dinner")));
        }

        var drawP95 = CalculatePercentile(drawSamples, 0.95);
        var searchP95 = CalculatePercentile(searchSamples, 0.95);

        Assert.True(
            drawP95 <= s_smokeP95Budget,
            $"Expected draw smoke p95 to stay within {s_smokeP95Budget.TotalMilliseconds} ms; actual {drawP95.TotalMilliseconds:F2} ms.");
        Assert.True(
            searchP95 <= s_smokeP95Budget,
            $"Expected search smoke p95 to stay within {s_smokeP95Budget.TotalMilliseconds} ms; actual {searchP95.TotalMilliseconds:F2} ms.");
    }

    [Fact]
    public async Task DrawAndSearchFlows_WhenMeasured_StayWithinSingleRequestAllocationBudget()
    {
        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(CreateCardsJson(CreateCards(180)));
        using var client = CreateHttpsClient(factory);

        var antiForgeryToken = await GetAntiForgeryTokenAsync(client, "/");

        await ExecuteDrawAsync(client, antiForgeryToken, MealType.Breakfast);
        await ExecuteSearchAsync(client, "/Cards?keyword=Meal&mealType=Breakfast");

        var drawAllocationBytes = await MeasureAllocatedBytesAsync(
            () => ExecuteDrawAsync(client, antiForgeryToken, MealType.Breakfast));
        var searchAllocationBytes = await MeasureAllocatedBytesAsync(
            () => ExecuteSearchAsync(client, "/Cards?keyword=Meal&mealType=Breakfast"));

        Assert.InRange(drawAllocationBytes, 0, SingleRequestAllocationBudgetBytes);
        Assert.InRange(searchAllocationBytes, 0, SingleRequestAllocationBudgetBytes);
    }

    private static HttpClient CreateHttpsClient(CardPickerWebApplicationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        return factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
            });
    }

    private static async Task<string> GetAntiForgeryTokenAsync(HttpClient client, string path)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return ExtractAntiForgeryToken(html);
    }

    private static async Task ExecuteDrawAsync(HttpClient client, string antiForgeryToken, MealType mealType)
    {
        var response = await client.PostAsync(
            "/?handler=Draw",
            new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("SelectedMealType", mealType.ToString()),
                new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken),
            ]));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Meal", html, StringComparison.Ordinal);
    }

    private static async Task ExecuteSearchAsync(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Meal", html, StringComparison.Ordinal);
    }

    private static async Task<TimeSpan> MeasureAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var stopwatch = Stopwatch.StartNew();
        await action();
        stopwatch.Stop();
        return stopwatch.Elapsed;
    }

    private static async Task<long> MeasureAllocatedBytesAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetTotalAllocatedBytes(precise: true);
        await action();
        var after = GC.GetTotalAllocatedBytes(precise: true);

        return after - before;
    }

    private static TimeSpan CalculatePercentile(IReadOnlyList<TimeSpan> samples, double percentile)
    {
        ArgumentNullException.ThrowIfNull(samples);

        var ordered = samples.OrderBy(sample => sample).ToArray();
        var index = Math.Max(0, (int)Math.Ceiling(ordered.Length * percentile) - 1);
        return ordered[index];
    }

    private static string ExtractAntiForgeryToken(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            throw new ArgumentException("HTML is required.", nameof(html));
        }

        var inputMatch = Regex.Match(
            html,
            """<input[^>]*name="__RequestVerificationToken"[^>]*>""",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        Assert.True(inputMatch.Success, "The page should render an anti-forgery token field.");

        var valueMatch = Regex.Match(
            inputMatch.Value,
            "value=\"([^\"]+)\"",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        Assert.True(valueMatch.Success, "The anti-forgery token field should contain a value.");

        return WebUtility.HtmlDecode(valueMatch.Groups[1].Value);
    }

    private static string CreateCardsJson(IEnumerable<object> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);

        return JsonSerializer.Serialize(
            new
            {
                schemaVersion = CardLibraryDocument.CurrentSchemaVersion,
                cards,
            });
    }

    private static IEnumerable<object> CreateCards(int cardCount)
    {
        if (cardCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cardCount));
        }

        var timestamp = new DateTimeOffset(2024, 1, 1, 8, 0, 0, TimeSpan.Zero);

        for (var index = 0; index < cardCount; index++)
        {
            var mealType = (index % 3) switch
            {
                0 => MealType.Breakfast,
                1 => MealType.Lunch,
                _ => MealType.Dinner,
            };

            yield return new
            {
                id = Guid.CreateVersion7().ToString("N"),
                name = $"{mealType} Meal {index:000}",
                mealType = mealType.ToString(),
                description = $"Smoke dataset card {index:000} for {mealType}.",
                createdAtUtc = timestamp,
                updatedAtUtc = timestamp,
            };
        }
    }
}
