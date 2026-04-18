using System.Text.Json;
using CardPicker.Models;
using CardPicker.Tests.Integration.Infrastructure;

namespace CardPicker.Tests.Integration.Pages;

public sealed class CardLibraryPageTests
{
    [Fact]
    public async Task GetAsync_WhenRequestingCardsPage_RendersDefaultSummaryWithoutDescriptions()
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

        var response = await client.GetAsync("/Cards");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("keyword", html, StringComparison.Ordinal);
        Assert.Contains("mealType", html, StringComparison.Ordinal);
        Assert.Contains("火腿蛋吐司", html, StringComparison.Ordinal);
        Assert.Contains("早餐", html, StringComparison.Ordinal);
        Assert.Contains("紅燒牛肉麵", html, StringComparison.Ordinal);
        Assert.Contains("午餐", html, StringComparison.Ordinal);
        Assert.Contains("鮭魚便當", html, StringComparison.Ordinal);
        Assert.Contains("晚餐", html, StringComparison.Ordinal);
        Assert.DoesNotContain("附近早餐店的招牌組合，五分鐘內可以外帶。", html, StringComparison.Ordinal);
        Assert.DoesNotContain("湯頭濃郁，適合想吃熱食又需要飽足感的中午。", html, StringComparison.Ordinal);
        Assert.DoesNotContain("有主菜、青菜和白飯，適合下班後快速解決晚餐。", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAsync_WhenRequestingCardsPage_RendersCrudEntryPointsForLibraryManagement()
    {
        const string firstCardId = "0195f2f4e7d47c6496ef0bbca4e6df6d";

        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(
            CreateCardsJson(
                CreateCard(
                    firstCardId,
                    "火腿蛋吐司",
                    MealType.Breakfast,
                    "附近早餐店的招牌組合，五分鐘內可以外帶。")));
        using var client = CreateHttpsClient(factory);

        var response = await client.GetAsync("/Cards");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("href=\"/Cards/Create\"", html, StringComparison.Ordinal);
        Assert.Contains($"href=\"/Cards/Edit?id={firstCardId}\"", html, StringComparison.Ordinal);
        Assert.Contains($"href=\"/Cards/Delete?id={firstCardId}\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAsync_WhenKeywordIsProvided_FiltersCardsByCaseInsensitivePartialName()
    {
        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(
            CreateCardsJson(
                CreateCard(
                    "0195f2f4e7d47c6496ef0bbca4e6df6d",
                    "Chicken Burger",
                    MealType.Lunch,
                    "酥脆雞排搭配生菜與醬汁。"),
                CreateCard(
                    "0195f2f5a1b67d1f9a2d4c9e1f0a2b3c",
                    "BURGER Salad",
                    MealType.Dinner,
                    "沙拉裡加上炙燒牛肉片。"),
                CreateCard(
                    "0195f2f63c897b2491c88d6e5f4a3210",
                    "Pasta Bowl",
                    MealType.Dinner,
                    "濃郁白醬義大利麵。")));
        using var client = CreateHttpsClient(factory);

        var response = await client.GetAsync("/Cards?keyword=burger");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Chicken Burger", html, StringComparison.Ordinal);
        Assert.Contains("BURGER Salad", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Pasta Bowl", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAsync_WhenMealTypeIsProvided_FiltersCardsByMealType()
    {
        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(
            CreateCardsJson(
                CreateCard(
                    "0195f2f4e7d47c6496ef0bbca4e6df6d",
                    "蛋餅",
                    MealType.Breakfast,
                    "快速外帶的早餐選項。"),
                CreateCard(
                    "0195f2f5a1b67d1f9a2d4c9e1f0a2b3c",
                    "牛肉麵",
                    MealType.Lunch,
                    "適合中午好好吃一碗熱湯麵。"),
                CreateCard(
                    "0195f2f63c897b2491c88d6e5f4a3210",
                    "鮭魚便當",
                    MealType.Dinner,
                    "有魚、有菜也有飯。")));
        using var client = CreateHttpsClient(factory);

        var response = await client.GetAsync("/Cards?mealType=Lunch");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("牛肉麵", html, StringComparison.Ordinal);
        Assert.Contains("午餐", html, StringComparison.Ordinal);
        Assert.DoesNotContain("蛋餅", html, StringComparison.Ordinal);
        Assert.DoesNotContain("鮭魚便當", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAsync_WhenKeywordAndMealTypeAreCombined_AppliesAndFiltering()
    {
        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(
            CreateCardsJson(
                CreateCard(
                    "0195f2f4e7d47c6496ef0bbca4e6df6d",
                    "Burger Deluxe",
                    MealType.Breakfast,
                    "早餐限定漢堡。"),
                CreateCard(
                    "0195f2f5a1b67d1f9a2d4c9e1f0a2b3c",
                    "Burger Deluxe",
                    MealType.Dinner,
                    "晚餐版本加大份量。"),
                CreateCard(
                    "0195f2f63c897b2491c88d6e5f4a3210",
                    "Steak Plate",
                    MealType.Dinner,
                    "適合聚餐時享用。")));
        using var client = CreateHttpsClient(factory);

        var response = await client.GetAsync("/Cards?keyword=burger&mealType=Dinner");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Burger Deluxe", html, StringComparison.Ordinal);
        Assert.Contains("晚餐", html, StringComparison.Ordinal);
        Assert.DoesNotContain("早餐限定漢堡。", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Steak Plate", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAsync_WhenClearingConditions_ResetsQueryAndShowsUnfilteredSummary()
    {
        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(
            CreateCardsJson(
                CreateCard(
                    "0195f2f4e7d47c6496ef0bbca4e6df6d",
                    "Chicken Burger",
                    MealType.Lunch,
                    "午餐時段的人氣漢堡。"),
                CreateCard(
                    "0195f2f5a1b67d1f9a2d4c9e1f0a2b3c",
                    "海鮮粥",
                    MealType.Dinner,
                    "適合晚餐想吃熱粥時。")));
        using var client = CreateHttpsClient(factory);

        var filteredResponse = await client.GetAsync("/Cards?keyword=burger&mealType=Lunch");
        var filteredHtml = await filteredResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, filteredResponse.StatusCode);
        Assert.Contains("清除條件", filteredHtml, StringComparison.Ordinal);
        Assert.True(
            filteredHtml.Contains("href=\"/Cards\"", StringComparison.Ordinal)
            || filteredHtml.Contains("formaction=\"/Cards\"", StringComparison.Ordinal)
            || filteredHtml.Contains("action=\"/Cards\"", StringComparison.Ordinal),
            "Expected the clear-condition control to target /Cards without preserving the query string.");

        var clearedResponse = await client.GetAsync("/Cards");
        var clearedHtml = await clearedResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, clearedResponse.StatusCode);
        Assert.Equal(string.Empty, clearedResponse.RequestMessage?.RequestUri?.Query);
        Assert.Contains("Chicken Burger", clearedHtml, StringComparison.Ordinal);
        Assert.Contains("海鮮粥", clearedHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAsync_WhenNoCardsMatch_ShowsEmptyResultMessage()
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

        var response = await client.GetAsync("/Cards?keyword=sushi&mealType=Dinner");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("查無符合條件的餐點卡牌。", html, StringComparison.Ordinal);
        Assert.DoesNotContain("火腿蛋吐司", html, StringComparison.Ordinal);
        Assert.DoesNotContain("紅燒牛肉麵", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAsync_WhenCardIsSelected_ShowsCardDetailContent()
    {
        const string selectedCardId = "0195f2f4e7d47c6496ef0bbca4e6df6d";

        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(
            CreateCardsJson(
                CreateCard(
                    selectedCardId,
                    "火腿蛋吐司",
                    MealType.Breakfast,
                    "附近早餐店的招牌組合，五分鐘內可以外帶。"),
                CreateCard(
                    "0195f2f5a1b67d1f9a2d4c9e1f0a2b3c",
                    "紅燒牛肉麵",
                    MealType.Lunch,
                    "湯頭濃郁，適合想吃熱食又需要飽足感的中午。")));
        using var client = CreateHttpsClient(factory);

        var response = await client.GetAsync($"/Cards?id={selectedCardId}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("火腿蛋吐司", html, StringComparison.Ordinal);
        Assert.Contains("早餐", html, StringComparison.Ordinal);
        Assert.Contains("附近早餐店的招牌組合，五分鐘內可以外帶。", html, StringComparison.Ordinal);
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
