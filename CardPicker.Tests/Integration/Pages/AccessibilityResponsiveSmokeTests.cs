using System.Text.Json;
using CardPicker.Models;
using CardPicker.Tests.Integration.Infrastructure;

namespace CardPicker.Tests.Integration.Pages;

public sealed class AccessibilityResponsiveSmokeTests
{
    private const string ExistingCardId = "0195f2f4e7d47c6496ef0bbca4e6df6d";

    [Fact]
    public async Task HomeAndLibraryPages_WhenRendered_ExposeSemanticLandmarksAndResponsiveMarkers()
    {
        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(
            CreateCardsJson(
                CreateCard(
                    ExistingCardId,
                    "火腿蛋吐司",
                    MealType.Breakfast,
                    "附近早餐店的招牌組合，五分鐘內可以外帶。")));
        using var client = CreateHttpsClient(factory);

        var homeResponse = await client.GetAsync("/");
        var homeHtml = await homeResponse.Content.ReadAsStringAsync();
        var libraryResponse = await client.GetAsync("/Cards");
        var libraryHtml = await libraryResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, homeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, libraryResponse.StatusCode);

        Assert.Contains("<html lang=\"zh-Hant\">", homeHtml, StringComparison.Ordinal);
        Assert.Contains("name=\"viewport\"", homeHtml, StringComparison.Ordinal);
        Assert.Contains("<main", homeHtml, StringComparison.Ordinal);
        Assert.Contains("role=\"main\"", homeHtml, StringComparison.Ordinal);
        Assert.Contains("navbar-expand-sm", homeHtml, StringComparison.Ordinal);
        Assert.Contains("draw-page__title", homeHtml, StringComparison.Ordinal);
        Assert.Contains("今天吃什麼？", homeHtml, StringComparison.Ordinal);
        Assert.Contains("draw-form__fieldset", homeHtml, StringComparison.Ordinal);
        Assert.Contains("請選擇餐別", homeHtml, StringComparison.Ordinal);

        Assert.Contains("library-page__title", libraryHtml, StringComparison.Ordinal);
        Assert.Contains("餐點卡牌庫", libraryHtml, StringComparison.Ordinal);
        Assert.Contains("flex-column flex-md-row", libraryHtml, StringComparison.Ordinal);
        Assert.Contains("for=\"keyword\"", libraryHtml, StringComparison.Ordinal);
        Assert.Contains("for=\"mealType\"", libraryHtml, StringComparison.Ordinal);
        Assert.Contains("href=\"/Cards/Create\"", libraryHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CoreInteractions_WhenRendered_UseKeyboardReachableNativeControlsAndAssociatedLabels()
    {
        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(
            CreateCardsJson(
                CreateCard(
                    ExistingCardId,
                    "火腿蛋吐司",
                    MealType.Breakfast,
                    "附近早餐店的招牌組合，五分鐘內可以外帶。")));
        using var client = CreateHttpsClient(factory);

        var homeHtml = await GetHtmlAsync(client, "/");
        var createHtml = await GetHtmlAsync(client, "/Cards/Create");
        var editHtml = await GetHtmlAsync(client, $"/Cards/Edit?id={ExistingCardId}");
        var deleteHtml = await GetHtmlAsync(client, $"/Cards/Delete?id={ExistingCardId}");

        Assert.Contains("id=\"mealType-breakfast\"", homeHtml, StringComparison.Ordinal);
        Assert.Contains("for=\"mealType-breakfast\"", homeHtml, StringComparison.Ordinal);
        Assert.Contains("id=\"mealType-lunch\"", homeHtml, StringComparison.Ordinal);
        Assert.Contains("for=\"mealType-lunch\"", homeHtml, StringComparison.Ordinal);
        Assert.Contains("id=\"mealType-dinner\"", homeHtml, StringComparison.Ordinal);
        Assert.Contains("for=\"mealType-dinner\"", homeHtml, StringComparison.Ordinal);
        Assert.Contains("draw-form__submit", homeHtml, StringComparison.Ordinal);
        Assert.Contains("抽一張", homeHtml, StringComparison.Ordinal);

        Assert.Contains("for=\"Input_Name\"", createHtml, StringComparison.Ordinal);
        Assert.Contains("id=\"Input_Name\"", createHtml, StringComparison.Ordinal);
        Assert.Contains("for=\"Input_MealType\"", createHtml, StringComparison.Ordinal);
        Assert.Contains("id=\"Input_MealType\"", createHtml, StringComparison.Ordinal);
        Assert.Contains("for=\"Input_Description\"", createHtml, StringComparison.Ordinal);
        Assert.Contains("id=\"Input_Description\"", createHtml, StringComparison.Ordinal);
        Assert.Contains("儲存卡牌", createHtml, StringComparison.Ordinal);

        Assert.Contains("for=\"Input_Name\"", editHtml, StringComparison.Ordinal);
        Assert.Contains("for=\"Input_MealType\"", editHtml, StringComparison.Ordinal);
        Assert.Contains("for=\"Input_Description\"", editHtml, StringComparison.Ordinal);
        Assert.Contains("儲存變更", editHtml, StringComparison.Ordinal);

        Assert.Contains("id=\"confirmDelete\"", deleteHtml, StringComparison.Ordinal);
        Assert.Contains("for=\"confirmDelete\"", deleteHtml, StringComparison.Ordinal);
        Assert.Contains("確認刪除", deleteHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateAndEditPages_WhenRendered_ExposeValidationAssociationsAndClientValidationHooks()
    {
        using var factory = CardPickerWebApplicationFactory.CreateWithCardsJson(
            CreateCardsJson(
                CreateCard(
                    ExistingCardId,
                    "火腿蛋吐司",
                    MealType.Breakfast,
                    "附近早餐店的招牌組合，五分鐘內可以外帶。")));
        using var client = CreateHttpsClient(factory);

        var createHtml = await GetHtmlAsync(client, "/Cards/Create");
        var editHtml = await GetHtmlAsync(client, $"/Cards/Edit?id={ExistingCardId}");

        AssertValidationMarkup(createHtml);
        AssertValidationMarkup(editHtml);

        Assert.Contains("/lib/jquery-validation/dist/jquery.validate.min.js", createHtml, StringComparison.Ordinal);
        Assert.Contains("/lib/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js", createHtml, StringComparison.Ordinal);
        Assert.Contains("/lib/jquery-validation/dist/jquery.validate.min.js", editHtml, StringComparison.Ordinal);
        Assert.Contains("/lib/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js", editHtml, StringComparison.Ordinal);
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

    private static async Task<string> GetHtmlAsync(HttpClient client, string path)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return html;
    }

    private static void AssertValidationMarkup(string html)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(html);

        Assert.Contains("data-val=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-valmsg-for=\"Input.Name\"", html, StringComparison.Ordinal);
        Assert.Contains("data-valmsg-for=\"Input.MealType\"", html, StringComparison.Ordinal);
        Assert.Contains("data-valmsg-for=\"Input.Description\"", html, StringComparison.Ordinal);
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
