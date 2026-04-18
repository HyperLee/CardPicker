using System.Text.Json;
using System.Text.RegularExpressions;
using CardPicker.Models;
using CardPicker.Tests.Integration.Infrastructure;

namespace CardPicker.Tests.Integration.Pages;

public sealed class CardManagementPageTests
{
    [Fact]
    public async Task GetCreateAsync_WhenRequestingPage_RendersCardFormAndAntiForgeryToken()
    {
        using var factory = new CardPickerWebApplicationFactory();
        using var client = CreateHttpsClient(factory);

        var response = await client.GetAsync("/Cards/Create");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Input.Name", html, StringComparison.Ordinal);
        Assert.Contains("Input.MealType", html, StringComparison.Ordinal);
        Assert.Contains("Input.Description", html, StringComparison.Ordinal);
        Assert.Contains("__RequestVerificationToken", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostCreateAsync_WhenAntiForgeryTokenIsMissing_ReturnsBadRequest()
    {
        using var factory = new CardPickerWebApplicationFactory();
        using var client = CreateHttpsClient(factory);

        var response = await client.PostAsync(
            "/Cards/Create",
            CreateCreateRequest("奶油培根義大利麵", MealType.Dinner, "白醬濃郁且份量足夠。"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostCreateAsync_WhenSaveFails_ShowsGenericErrorAndPreservesInput()
    {
        using var factory = new CardPickerWebApplicationFactory();
        using var client = CreateHttpsClient(factory);

        var antiForgeryToken = await GetAntiForgeryTokenAsync(client, "/Cards/Create");
        factory.CardDataDirectory.DeleteCardsFile();
        Directory.CreateDirectory(factory.CardDataDirectory.CardsFilePath);

        var response = await client.PostAsync(
            "/Cards/Create",
            CreateCreateRequest(
                "奶油培根義大利麵",
                MealType.Dinner,
                "白醬濃郁且份量足夠。",
                antiForgeryToken));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("失敗", html, StringComparison.Ordinal);
        Assert.Contains("奶油培根義大利麵", html, StringComparison.Ordinal);
        Assert.Contains("白醬濃郁且份量足夠。", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostCreateAsync_WhenInputIsValid_PersistsToLibraryDrawAndRestart()
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

        var antiForgeryToken = await GetAntiForgeryTokenAsync(client, "/Cards/Create");
        var response = await client.PostAsync(
            "/Cards/Create",
            CreateCreateRequest(
                "奶油培根義大利麵",
                MealType.Dinner,
                "白醬濃郁且份量足夠。",
                antiForgeryToken));

        AssertRedirectsToCards(response);

        var redirectedResponse = await client.GetAsync(response.Headers.Location);
        var redirectedHtml = await redirectedResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, redirectedResponse.StatusCode);
        Assert.Contains("已成功新增餐點卡牌。", redirectedHtml, StringComparison.Ordinal);

        var libraryResponse = await client.GetAsync("/Cards?keyword=%E5%A5%B6%E6%B2%B9%E5%9F%B9%E6%A0%B9");
        var libraryHtml = await libraryResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, libraryResponse.StatusCode);
        Assert.Contains("奶油培根義大利麵", libraryHtml, StringComparison.Ordinal);
        Assert.Contains("晚餐", libraryHtml, StringComparison.Ordinal);

        var drawAntiForgeryToken = await GetAntiForgeryTokenAsync(client, "/");
        var drawResponse = await client.PostAsync(
            "/?handler=Draw",
            CreateDrawRequest(MealType.Dinner, drawAntiForgeryToken));
        var drawHtml = await drawResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, drawResponse.StatusCode);
        Assert.Contains("奶油培根義大利麵", drawHtml, StringComparison.Ordinal);
        Assert.Contains("白醬濃郁且份量足夠。", drawHtml, StringComparison.Ordinal);

        using var restartedFactory = CardPickerWebApplicationFactory.CreateWithCardsJson(factory.CardDataDirectory.ReadCardsJson());
        using var restartedClient = CreateHttpsClient(restartedFactory);
        var restartedResponse = await restartedClient.GetAsync("/Cards?keyword=%E5%A5%B6%E6%B2%B9%E5%9F%B9%E6%A0%B9");
        var restartedHtml = await restartedResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, restartedResponse.StatusCode);
        Assert.Contains("奶油培根義大利麵", restartedHtml, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/Cards/Edit")]
    [InlineData("/Cards/Delete")]
    public async Task GetAsync_WhenIdIsMissing_ShowsCardNotFoundGuidance(string path)
    {
        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(
            CreateCardsJson(
                CreateCard(
                    "0195f2f4e7d47c6496ef0bbca4e6df6d",
                    "火腿蛋吐司",
                    MealType.Breakfast,
                    "附近早餐店的招牌組合，五分鐘內可以外帶。")));
        using var client = CreateHttpsClient(factory);

        var response = await client.GetAsync(path);

        await AssertMissingCardResponseAsync(client, response);
    }

    [Theory]
    [InlineData("/Cards/Edit?id=0195f2f63c897b2491c88d6e5f4a3210")]
    [InlineData("/Cards/Delete?id=0195f2f63c897b2491c88d6e5f4a3210")]
    public async Task GetAsync_WhenIdDoesNotExist_ShowsCardNotFoundGuidance(string path)
    {
        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(
            CreateCardsJson(
                CreateCard(
                    "0195f2f4e7d47c6496ef0bbca4e6df6d",
                    "火腿蛋吐司",
                    MealType.Breakfast,
                    "附近早餐店的招牌組合，五分鐘內可以外帶。")));
        using var client = CreateHttpsClient(factory);

        var response = await client.GetAsync(path);

        await AssertMissingCardResponseAsync(client, response);
    }

    [Fact]
    public async Task GetEditAsync_WhenCardExists_PreloadsValuesAndDoesNotExposeEditableIdField()
    {
        const string cardId = "0195f2f4e7d47c6496ef0bbca4e6df6d";

        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(
            CreateCardsJson(
                CreateCard(
                    cardId,
                    "火腿蛋吐司",
                    MealType.Breakfast,
                    "附近早餐店的招牌組合，五分鐘內可以外帶。")));
        using var client = CreateHttpsClient(factory);

        var response = await client.GetAsync($"/Cards/Edit?id={cardId}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("火腿蛋吐司", html, StringComparison.Ordinal);
        Assert.Contains("附近早餐店的招牌組合，五分鐘內可以外帶。", html, StringComparison.Ordinal);
        Assert.Contains("__RequestVerificationToken", html, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"Input.Id\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostEditAsync_WhenAntiForgeryTokenIsMissing_ReturnsBadRequest()
    {
        const string cardId = "0195f2f4e7d47c6496ef0bbca4e6df6d";

        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(
            CreateCardsJson(
                CreateCard(
                    cardId,
                    "火腿蛋吐司",
                    MealType.Breakfast,
                    "附近早餐店的招牌組合，五分鐘內可以外帶。")));
        using var client = CreateHttpsClient(factory);

        var response = await client.PostAsync(
            $"/Cards/Edit?id={cardId}",
            CreateEditRequest(
                cardId,
                "香煎雞腿排",
                MealType.Dinner,
                "外皮酥脆，適合晚餐。"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostEditAsync_WhenInputIsValid_UpdatesLibraryDrawAndRestart()
    {
        const string cardId = "0195f2f4e7d47c6496ef0bbca4e6df6d";

        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(
            CreateCardsJson(
                CreateCard(
                    cardId,
                    "火腿蛋吐司",
                    MealType.Breakfast,
                    "附近早餐店的招牌組合，五分鐘內可以外帶。")));
        using var client = CreateHttpsClient(factory);

        var antiForgeryToken = await GetAntiForgeryTokenAsync(client, $"/Cards/Edit?id={cardId}");
        var response = await client.PostAsync(
            $"/Cards/Edit?id={cardId}",
            CreateEditRequest(
                cardId,
                "香煎雞腿排",
                MealType.Dinner,
                "外皮酥脆，適合晚餐。",
                antiForgeryToken));

        AssertRedirectsToCards(response);

        var redirectedResponse = await client.GetAsync(response.Headers.Location);
        var redirectedHtml = await redirectedResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, redirectedResponse.StatusCode);
        Assert.Contains("已成功更新餐點卡牌。", redirectedHtml, StringComparison.Ordinal);

        var libraryResponse = await client.GetAsync("/Cards?keyword=%E9%A6%99%E7%85%8E%E9%9B%9E%E8%85%BF%E6%8E%92");
        var libraryHtml = await libraryResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, libraryResponse.StatusCode);
        Assert.Contains("香煎雞腿排", libraryHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("火腿蛋吐司", libraryHtml, StringComparison.Ordinal);

        var drawAntiForgeryToken = await GetAntiForgeryTokenAsync(client, "/");
        var drawResponse = await client.PostAsync(
            "/?handler=Draw",
            CreateDrawRequest(MealType.Dinner, drawAntiForgeryToken));
        var drawHtml = await drawResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, drawResponse.StatusCode);
        Assert.Contains("香煎雞腿排", drawHtml, StringComparison.Ordinal);
        Assert.Contains("外皮酥脆，適合晚餐。", drawHtml, StringComparison.Ordinal);

        using var restartedFactory = CardPickerWebApplicationFactory.CreateWithCardsJson(factory.CardDataDirectory.ReadCardsJson());
        using var restartedClient = CreateHttpsClient(restartedFactory);
        var restartedResponse = await restartedClient.GetAsync("/Cards?keyword=%E9%A6%99%E7%85%8E%E9%9B%9E%E8%85%BF%E6%8E%92");
        var restartedHtml = await restartedResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, restartedResponse.StatusCode);
        Assert.Contains("香煎雞腿排", restartedHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetDeleteAsync_WhenCardExists_RendersConfirmationSummaryAndAntiForgeryToken()
    {
        const string cardId = "0195f2f4e7d47c6496ef0bbca4e6df6d";

        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(
            CreateCardsJson(
                CreateCard(
                    cardId,
                    "火腿蛋吐司",
                    MealType.Breakfast,
                    "附近早餐店的招牌組合，五分鐘內可以外帶。")));
        using var client = CreateHttpsClient(factory);

        var response = await client.GetAsync($"/Cards/Delete?id={cardId}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("火腿蛋吐司", html, StringComparison.Ordinal);
        Assert.Contains("早餐", html, StringComparison.Ordinal);
        Assert.Contains("附近早餐店的招牌組合，五分鐘內可以外帶。", html, StringComparison.Ordinal);
        Assert.Contains("confirmDelete", html, StringComparison.Ordinal);
        Assert.Contains("__RequestVerificationToken", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostDeleteAsync_WhenConfirmationIsMissing_KeepsCardAndReturnsConfirmationPage()
    {
        const string cardId = "0195f2f4e7d47c6496ef0bbca4e6df6d";

        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(
            CreateCardsJson(
                CreateCard(
                    cardId,
                    "火腿蛋吐司",
                    MealType.Breakfast,
                    "附近早餐店的招牌組合，五分鐘內可以外帶。")));
        using var client = CreateHttpsClient(factory);

        var antiForgeryToken = await GetAntiForgeryTokenAsync(client, $"/Cards/Delete?id={cardId}");
        var response = await client.PostAsync(
            $"/Cards/Delete?id={cardId}",
            CreateDeleteRequest(cardId, antiForgeryToken: antiForgeryToken));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("火腿蛋吐司", html, StringComparison.Ordinal);

        var libraryResponse = await client.GetAsync("/Cards?keyword=%E7%81%AB%E8%85%BF%E8%9B%8B%E5%90%90%E5%8F%B8");
        var libraryHtml = await libraryResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, libraryResponse.StatusCode);
        Assert.Contains("火腿蛋吐司", libraryHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostDeleteAsync_WhenAntiForgeryTokenIsMissing_ReturnsBadRequest()
    {
        const string cardId = "0195f2f4e7d47c6496ef0bbca4e6df6d";

        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(
            CreateCardsJson(
                CreateCard(
                    cardId,
                    "火腿蛋吐司",
                    MealType.Breakfast,
                    "附近早餐店的招牌組合，五分鐘內可以外帶。")));
        using var client = CreateHttpsClient(factory);

        var response = await client.PostAsync(
            $"/Cards/Delete?id={cardId}",
            CreateDeleteRequest(cardId, confirmDelete: true));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostDeleteAsync_WhenSaveFails_ShowsGenericErrorAndKeepsConfirmationSummary()
    {
        const string cardId = "0195f2f4e7d47c6496ef0bbca4e6df6d";

        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(
            CreateCardsJson(
                CreateCard(
                    cardId,
                    "火腿蛋吐司",
                    MealType.Breakfast,
                    "附近早餐店的招牌組合，五分鐘內可以外帶。")));
        using var client = CreateHttpsClient(factory);

        var antiForgeryToken = await GetAntiForgeryTokenAsync(client, $"/Cards/Delete?id={cardId}");
        factory.CardDataDirectory.DeleteCardsFile();
        Directory.CreateDirectory(factory.CardDataDirectory.CardsFilePath);

        var response = await client.PostAsync(
            $"/Cards/Delete?id={cardId}",
            CreateDeleteRequest(cardId, confirmDelete: true, antiForgeryToken));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("失敗", html, StringComparison.Ordinal);
        Assert.Contains("火腿蛋吐司", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostDeleteAsync_WhenConfirmed_RemovesCardFromLibraryDrawAndRestart()
    {
        const string deletedCardId = "0195f2f63c897b2491c88d6e5f4a3210";

        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(
            CreateCardsJson(
                CreateCard(
                    "0195f2f4e7d47c6496ef0bbca4e6df6d",
                    "火腿蛋吐司",
                    MealType.Breakfast,
                    "附近早餐店的招牌組合，五分鐘內可以外帶。"),
                CreateCard(
                    deletedCardId,
                    "鮭魚便當",
                    MealType.Dinner,
                    "有主菜、青菜和白飯，適合下班後快速解決晚餐。")));
        using var client = CreateHttpsClient(factory);

        var antiForgeryToken = await GetAntiForgeryTokenAsync(client, $"/Cards/Delete?id={deletedCardId}");
        var response = await client.PostAsync(
            $"/Cards/Delete?id={deletedCardId}",
            CreateDeleteRequest(deletedCardId, confirmDelete: true, antiForgeryToken));

        AssertRedirectsToCards(response);

        var redirectedResponse = await client.GetAsync(response.Headers.Location);
        var redirectedHtml = await redirectedResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, redirectedResponse.StatusCode);
        Assert.Contains("已成功刪除餐點卡牌。", redirectedHtml, StringComparison.Ordinal);

        var libraryResponse = await client.GetAsync("/Cards");
        var libraryHtml = await libraryResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, libraryResponse.StatusCode);
        Assert.DoesNotContain("鮭魚便當", libraryHtml, StringComparison.Ordinal);
        Assert.Contains("火腿蛋吐司", libraryHtml, StringComparison.Ordinal);

        var drawAntiForgeryToken = await GetAntiForgeryTokenAsync(client, "/");
        var drawResponse = await client.PostAsync(
            "/?handler=Draw",
            CreateDrawRequest(MealType.Dinner, drawAntiForgeryToken));
        var drawHtml = await drawResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, drawResponse.StatusCode);
        Assert.Contains("目前沒有可抽取的餐點。", drawHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("鮭魚便當", drawHtml, StringComparison.Ordinal);

        using var restartedFactory = CardPickerWebApplicationFactory.CreateWithCardsJson(factory.CardDataDirectory.ReadCardsJson());
        using var restartedClient = CreateHttpsClient(restartedFactory);
        var restartedResponse = await restartedClient.GetAsync("/Cards");
        var restartedHtml = await restartedResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, restartedResponse.StatusCode);
        Assert.DoesNotContain("鮭魚便當", restartedHtml, StringComparison.Ordinal);
        Assert.Contains("火腿蛋吐司", restartedHtml, StringComparison.Ordinal);
    }

    private static HttpClient CreateHttpsClient(CardPickerWebApplicationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        return factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
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

    private static FormUrlEncodedContent CreateCreateRequest(
        string name,
        MealType mealType,
        string description,
        string? antiForgeryToken = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(description);

        var values = new List<KeyValuePair<string, string>>
        {
            new("Input.Name", name),
            new("Input.MealType", mealType.ToString()),
            new("Input.Description", description),
        };

        if (!string.IsNullOrWhiteSpace(antiForgeryToken))
        {
            values.Add(new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken));
        }

        return new FormUrlEncodedContent(values);
    }

    private static FormUrlEncodedContent CreateEditRequest(
        string cardId,
        string name,
        MealType mealType,
        string description,
        string? antiForgeryToken = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cardId);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(description);

        var values = new List<KeyValuePair<string, string>>
        {
            new("id", cardId),
            new("Input.Name", name),
            new("Input.MealType", mealType.ToString()),
            new("Input.Description", description),
        };

        if (!string.IsNullOrWhiteSpace(antiForgeryToken))
        {
            values.Add(new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken));
        }

        return new FormUrlEncodedContent(values);
    }

    private static FormUrlEncodedContent CreateDeleteRequest(
        string cardId,
        bool confirmDelete = false,
        string? antiForgeryToken = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cardId);

        var values = new List<KeyValuePair<string, string>>
        {
            new("id", cardId),
        };

        if (confirmDelete)
        {
            values.Add(new KeyValuePair<string, string>("confirmDelete", bool.TrueString));
        }

        if (!string.IsNullOrWhiteSpace(antiForgeryToken))
        {
            values.Add(new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken));
        }

        return new FormUrlEncodedContent(values);
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

    private static async Task AssertMissingCardResponseAsync(HttpClient client, HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(response);

        var html = await response.Content.ReadAsStringAsync();

        if (IsRedirectToCards(response))
        {
            var redirectedResponse = await client.GetAsync(response.Headers.Location);
            var redirectedHtml = await redirectedResponse.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, redirectedResponse.StatusCode);
            Assert.Contains("不存在", redirectedHtml, StringComparison.Ordinal);
            return;
        }

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(
            html.Contains("不存在", StringComparison.Ordinal)
            || html.Contains("Not Found", StringComparison.Ordinal),
            "Expected a user-facing not-found response for a missing or nonexistent card ID.");
    }

    private static void AssertRedirectsToCards(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        Assert.True(
            response.StatusCode is HttpStatusCode.Found or HttpStatusCode.SeeOther or HttpStatusCode.RedirectMethod,
            $"Expected a redirect to /Cards but received {(int)response.StatusCode} {response.StatusCode}.");
        Assert.NotNull(response.Headers.Location);

        var redirectedPath = response.Headers.Location!.IsAbsoluteUri
            ? response.Headers.Location.AbsolutePath
            : response.Headers.Location.OriginalString.Split('?', 2, StringSplitOptions.None)[0];
        Assert.Equal("/Cards", redirectedPath);
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

    private static bool IsRedirectToCards(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.Headers.Location is null)
        {
            return false;
        }

        var redirectedPath = response.Headers.Location.IsAbsoluteUri
            ? response.Headers.Location.AbsolutePath
            : response.Headers.Location.OriginalString.Split('?', 2, StringSplitOptions.None)[0];

        return string.Equals(redirectedPath, "/Cards", StringComparison.Ordinal);
    }
}
