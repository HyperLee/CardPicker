using System.Text.Json;
using System.Text.RegularExpressions;
using CardPicker.Models;
using CardPicker.Tests.Integration.Infrastructure;

namespace CardPicker.Tests.Integration.Pages;

public sealed class QuickstartSmokeTests
{
    [Fact]
    public async Task QuickstartFlow_WhenExercisingMainJourney_ReflectsDrawSearchCrudAndPersistenceState()
    {
        const string breakfastCardId = "0195f2f4e7d47c6496ef0bbca4e6df6d";
        const string lunchCardId = "0195f2f5a1b67d1f9a2d4c9e1f0a2b3c";
        const string createdCardName = "Smokehouse Burger Bowl";
        const string createdCardDescription = "炙燒煙燻風味，適合想吃飽的晚餐。";
        const string updatedCardName = "Smokehouse Burger Deluxe";
        const string updatedCardDescription = "炙燒煙燻風味再升級，晚餐份量更有飽足感。";

        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(
            CreateCardsJson(
                CreateCard(
                    breakfastCardId,
                    "火腿蛋吐司",
                    MealType.Breakfast,
                    "附近早餐店的招牌組合，五分鐘內可以外帶。"),
                CreateCard(
                    lunchCardId,
                    "Burger Bento",
                    MealType.Lunch,
                    "漢堡排搭配白飯，方便午餐快速解決。")));
        using var client = CreateHttpsClient(factory);

        var breakfastDrawToken = await GetAntiForgeryTokenAsync(client, "/");
        var breakfastDrawResponse = await client.PostAsync(
            "/?handler=Draw",
            CreateDrawRequest(MealType.Breakfast, breakfastDrawToken));
        var breakfastDrawHtml = await breakfastDrawResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, breakfastDrawResponse.StatusCode);
        Assert.Contains("火腿蛋吐司", breakfastDrawHtml, StringComparison.Ordinal);
        Assert.Contains("早餐", breakfastDrawHtml, StringComparison.Ordinal);
        Assert.Contains("附近早餐店的招牌組合，五分鐘內可以外帶。", breakfastDrawHtml, StringComparison.Ordinal);

        var searchResponse = await client.GetAsync("/Cards?keyword=BURGER&mealType=Lunch");
        var searchHtml = await searchResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        Assert.Contains("Burger Bento", searchHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("火腿蛋吐司", searchHtml, StringComparison.Ordinal);

        var createToken = await GetAntiForgeryTokenAsync(client, "/Cards/Create");
        var createResponse = await client.PostAsync(
            "/Cards/Create",
            CreateCreateRequest(
                createdCardName,
                MealType.Dinner,
                createdCardDescription,
                createToken));

        AssertRedirectsToCards(createResponse);

        var createdCardId = ReadCardIdByName(factory.CardDataDirectory.ReadCardsJson(), createdCardName);
        var createdSearchResponse = await client.GetAsync("/Cards?keyword=burger&mealType=Dinner");
        var createdSearchHtml = await createdSearchResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, createdSearchResponse.StatusCode);
        Assert.Contains(createdCardName, createdSearchHtml, StringComparison.Ordinal);
        Assert.Contains("晚餐", createdSearchHtml, StringComparison.Ordinal);

        var dinnerDrawToken = await GetAntiForgeryTokenAsync(client, "/");
        var dinnerDrawResponse = await client.PostAsync(
            "/?handler=Draw",
            CreateDrawRequest(MealType.Dinner, dinnerDrawToken));
        var dinnerDrawHtml = await dinnerDrawResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, dinnerDrawResponse.StatusCode);
        Assert.Contains(createdCardName, dinnerDrawHtml, StringComparison.Ordinal);
        Assert.Contains(createdCardDescription, dinnerDrawHtml, StringComparison.Ordinal);

        var editToken = await GetAntiForgeryTokenAsync(client, $"/Cards/Edit?id={createdCardId}");
        var editResponse = await client.PostAsync(
            $"/Cards/Edit?id={createdCardId}",
            CreateEditRequest(
                createdCardId,
                updatedCardName,
                MealType.Dinner,
                updatedCardDescription,
                editToken));

        AssertRedirectsToCards(editResponse);

        var updatedSearchResponse = await client.GetAsync("/Cards?keyword=DELUXE&mealType=Dinner");
        var updatedSearchHtml = await updatedSearchResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, updatedSearchResponse.StatusCode);
        Assert.Contains(updatedCardName, updatedSearchHtml, StringComparison.Ordinal);
        Assert.DoesNotContain(createdCardName, updatedSearchHtml, StringComparison.Ordinal);

        var updatedDinnerDrawToken = await GetAntiForgeryTokenAsync(client, "/");
        var updatedDinnerDrawResponse = await client.PostAsync(
            "/?handler=Draw",
            CreateDrawRequest(MealType.Dinner, updatedDinnerDrawToken));
        var updatedDinnerDrawHtml = await updatedDinnerDrawResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, updatedDinnerDrawResponse.StatusCode);
        Assert.Contains(updatedCardName, updatedDinnerDrawHtml, StringComparison.Ordinal);
        Assert.Contains(updatedCardDescription, updatedDinnerDrawHtml, StringComparison.Ordinal);

        var deleteToken = await GetAntiForgeryTokenAsync(client, $"/Cards/Delete?id={createdCardId}");
        var deleteResponse = await client.PostAsync(
            $"/Cards/Delete?id={createdCardId}",
            CreateDeleteRequest(createdCardId, confirmDelete: true, deleteToken));

        AssertRedirectsToCards(deleteResponse);

        var deletedSearchResponse = await client.GetAsync("/Cards?keyword=burger&mealType=Dinner");
        var deletedSearchHtml = await deletedSearchResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, deletedSearchResponse.StatusCode);
        Assert.Contains("查無符合條件的餐點卡牌。", deletedSearchHtml, StringComparison.Ordinal);
        Assert.DoesNotContain(updatedCardName, deletedSearchHtml, StringComparison.Ordinal);

        var emptyDinnerDrawToken = await GetAntiForgeryTokenAsync(client, "/");
        var emptyDinnerDrawResponse = await client.PostAsync(
            "/?handler=Draw",
            CreateDrawRequest(MealType.Dinner, emptyDinnerDrawToken));
        var emptyDinnerDrawHtml = await emptyDinnerDrawResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, emptyDinnerDrawResponse.StatusCode);
        Assert.Contains("目前沒有可抽取的餐點。", emptyDinnerDrawHtml, StringComparison.Ordinal);
        Assert.DoesNotContain(updatedCardName, emptyDinnerDrawHtml, StringComparison.Ordinal);

        using var restartedFactory = CardPickerWebApplicationFactory.CreateWithCardsJson(factory.CardDataDirectory.ReadCardsJson());
        using var restartedClient = CreateHttpsClient(restartedFactory);
        var restartedResponse = await restartedClient.GetAsync("/Cards");
        var restartedHtml = await restartedResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, restartedResponse.StatusCode);
        Assert.Contains("火腿蛋吐司", restartedHtml, StringComparison.Ordinal);
        Assert.Contains("Burger Bento", restartedHtml, StringComparison.Ordinal);
        Assert.DoesNotContain(updatedCardName, restartedHtml, StringComparison.Ordinal);
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
        string antiForgeryToken)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(description);

        return new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("Input.Name", name),
            new KeyValuePair<string, string>("Input.MealType", mealType.ToString()),
            new KeyValuePair<string, string>("Input.Description", description),
            new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken),
        ]);
    }

    private static FormUrlEncodedContent CreateEditRequest(
        string cardId,
        string name,
        MealType mealType,
        string description,
        string antiForgeryToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cardId);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(description);

        return new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("id", cardId),
            new KeyValuePair<string, string>("Input.Name", name),
            new KeyValuePair<string, string>("Input.MealType", mealType.ToString()),
            new KeyValuePair<string, string>("Input.Description", description),
            new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken),
        ]);
    }

    private static FormUrlEncodedContent CreateDeleteRequest(
        string cardId,
        bool confirmDelete,
        string antiForgeryToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cardId);

        return new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("id", cardId),
            new KeyValuePair<string, string>("confirmDelete", confirmDelete.ToString()),
            new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken),
        ]);
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

    private static string ReadCardIdByName(string cardsJson, string cardName)
    {
        ArgumentNullException.ThrowIfNull(cardsJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(cardName);

        using var document = JsonDocument.Parse(cardsJson);
        foreach (var card in document.RootElement.GetProperty("cards").EnumerateArray())
        {
            if (string.Equals(card.GetProperty("name").GetString(), cardName, StringComparison.Ordinal))
            {
                return card.GetProperty("id").GetString()
                    ?? throw new InvalidOperationException("The stored card id should not be null.");
            }
        }

        throw new InvalidOperationException($"Unable to find card '{cardName}' in the persisted JSON document.");
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
