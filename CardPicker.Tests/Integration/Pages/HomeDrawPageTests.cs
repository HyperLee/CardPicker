using System.Text.Json;
using System.Text.RegularExpressions;
using CardPicker.Models;
using CardPicker.Tests.Integration.Infrastructure;

namespace CardPicker.Tests.Integration.Pages;

public sealed class HomeDrawPageTests
{
    [Fact]
    public async Task GetAsync_WhenRequestingHomePage_RendersDrawFormMealChoicesAndAntiForgeryToken()
    {
        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(
            CreateCardsJson(
                CreateCard(
                    "0195f2f4e7d47c6496ef0bbca4e6df6d",
                    "火腿蛋吐司",
                    MealType.Breakfast,
                    "附近早餐店的招牌組合，五分鐘內可以外帶。"),
                CreateCard(
                    "0195f2f5a1b67d1f9a2d4c9e1f0a2b3c",
                    "紅燒牛肉麵",
                    MealType.Lunch,
                    "湯頭濃郁，適合想吃熱食又需要飽足感的中午。"),
                CreateCard(
                    "0195f2f63c897b2491c88d6e5f4a3210",
                    "鮭魚便當",
                    MealType.Dinner,
                    "有主菜、青菜和白飯，適合下班後快速解決晚餐。")));
        using var client = CreateHttpsClient(factory);

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("SelectedMealType", html, StringComparison.Ordinal);
        Assert.Contains("早餐", html, StringComparison.Ordinal);
        Assert.Contains("午餐", html, StringComparison.Ordinal);
        Assert.Contains("晚餐", html, StringComparison.Ordinal);
        Assert.Contains("抽一張", html, StringComparison.Ordinal);
        Assert.Contains("__RequestVerificationToken", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/Cards/Create\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostAsync_WhenDrawRequestIncludesAntiForgeryToken_RendersDrawnCardDetails()
    {
        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(
            CreateCardsJson(
                CreateCard(
                    "0195f2f4e7d47c6496ef0bbca4e6df6d",
                    "火腿蛋吐司",
                    MealType.Breakfast,
                    "附近早餐店的招牌組合，五分鐘內可以外帶。")));
        using var client = CreateHttpsClient(factory);

        var antiForgeryToken = await GetAntiForgeryTokenAsync(client);
        var response = await client.PostAsync(
            "/?handler=Draw",
            CreateDrawRequest(MealType.Breakfast, antiForgeryToken));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("火腿蛋吐司", html, StringComparison.Ordinal);
        Assert.Contains("早餐", html, StringComparison.Ordinal);
        Assert.Contains("附近早餐店的招牌組合，五分鐘內可以外帶。", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostAsync_WhenMealTypeIsMissing_ShowsValidationMessageAndKeepsHomePage()
    {
        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(
            CreateCardsJson(
                CreateCard(
                    "0195f2f4e7d47c6496ef0bbca4e6df6d",
                    "火腿蛋吐司",
                    MealType.Breakfast,
                    "附近早餐店的招牌組合，五分鐘內可以外帶。")));
        using var client = CreateHttpsClient(factory);

        var antiForgeryToken = await GetAntiForgeryTokenAsync(client);
        var response = await client.PostAsync(
            "/?handler=Draw",
            new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken),
            ]));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("請先選擇餐別。", html, StringComparison.Ordinal);
        Assert.DoesNotContain("附近早餐店的招牌組合，五分鐘內可以外帶。", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostAsync_WhenSelectedMealTypeHasNoCards_ShowsEmptyPoolMessageWithoutFakeResult()
    {
        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(
            CreateCardsJson(
                CreateCard(
                    "0195f2f4e7d47c6496ef0bbca4e6df6d",
                    "火腿蛋吐司",
                    MealType.Breakfast,
                    "附近早餐店的招牌組合，五分鐘內可以外帶。"),
                CreateCard(
                    "0195f2f5a1b67d1f9a2d4c9e1f0a2b3c",
                    "紅燒牛肉麵",
                    MealType.Lunch,
                    "湯頭濃郁，適合想吃熱食又需要飽足感的中午。")));
        using var client = CreateHttpsClient(factory);

        var antiForgeryToken = await GetAntiForgeryTokenAsync(client);
        var response = await client.PostAsync(
            "/?handler=Draw",
            CreateDrawRequest(MealType.Dinner, antiForgeryToken));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("目前沒有可抽取的餐點。", html, StringComparison.Ordinal);
        Assert.DoesNotContain("火腿蛋吐司", html, StringComparison.Ordinal);
        Assert.DoesNotContain("紅燒牛肉麵", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HomePage_WhenRenderingAndPosting_AppliesCspAndEncodesDrawnCardContent()
    {
        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(
            CreateCardsJson(
                CreateCard(
                    "0195f2f4e7d47c6496ef0bbca4e6df6d",
                    "<script>alert(1)</script>",
                    MealType.Breakfast,
                    "<b>酥脆外皮</b>")));
        using var client = CreateHttpsClient(factory);

        var getResponse = await client.GetAsync("/");
        var cspHeader = Assert.Single(getResponse.Headers.GetValues("Content-Security-Policy"));
        Assert.Contains("default-src 'self'", cspHeader, StringComparison.Ordinal);
        Assert.Contains("form-action 'self'", cspHeader, StringComparison.Ordinal);
        Assert.Contains("frame-ancestors 'none'", cspHeader, StringComparison.Ordinal);
        Assert.Contains("object-src 'none'", cspHeader, StringComparison.Ordinal);

        var antiForgeryToken = ExtractAntiForgeryToken(await getResponse.Content.ReadAsStringAsync());
        var postResponse = await client.PostAsync(
            "/?handler=Draw",
            CreateDrawRequest(MealType.Breakfast, antiForgeryToken));
        var html = await postResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
        Assert.DoesNotContain("<script>alert(1)</script>", html, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<b>酥脆外皮</b>", html, StringComparison.Ordinal);
        Assert.Contains("&lt;b&gt;酥脆外皮&lt;/b&gt;", html, StringComparison.Ordinal);
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

    private static async Task<string> GetAntiForgeryTokenAsync(HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return ExtractAntiForgeryToken(html);
    }

    private static FormUrlEncodedContent CreateDrawRequest(MealType mealType, string antiForgeryToken)
    {
        if (string.IsNullOrWhiteSpace(antiForgeryToken))
        {
            throw new ArgumentException("A non-empty anti-forgery token is required.", nameof(antiForgeryToken));
        }

        return new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("SelectedMealType", mealType.ToString()),
            new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken),
        ]);
    }

    private static string ExtractAntiForgeryToken(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            throw new ArgumentException("Home page HTML is required.", nameof(html));
        }

        var inputMatch = Regex.Match(
            html,
            """<input[^>]*name="__RequestVerificationToken"[^>]*>""",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        Assert.True(inputMatch.Success, "The home page should render an anti-forgery token field.");

        var valueMatch = Regex.Match(
            inputMatch.Value,
            "value=\"([^\"]+)\"",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        Assert.True(valueMatch.Success, "The home page anti-forgery token field should contain a value.");

        return WebUtility.HtmlDecode(valueMatch.Groups[1].Value);
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
