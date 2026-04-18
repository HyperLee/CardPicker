using System.IO;
using CardPicker.Models;
using CardPicker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CardPicker.Pages.Cards;

public sealed class DeleteModel : PageModel
{
    private const string DeleteFailedMessage = "刪除餐點卡牌失敗，請稍後再試。";
    private const string DeleteSucceededMessage = "已成功刪除餐點卡牌。";

    private readonly IMealCardService _mealCardService;

    public DeleteModel(IMealCardService mealCardService)
    {
        ArgumentNullException.ThrowIfNull(mealCardService);

        _mealCardService = mealCardService;
    }

    [BindProperty(SupportsGet = true, Name = "id")]
    public string? CardId { get; set; }

    [BindProperty(Name = "confirmDelete")]
    public bool ConfirmDelete { get; set; }

    public MealCard? Card { get; private set; }

    public string? FailureMessage { get; private set; }

    [TempData]
    public string? CachedCardId { get; set; }

    [TempData]
    public string? CachedCardName { get; set; }

    [TempData]
    public string? CachedMealType { get; set; }

    [TempData]
    public string? CachedDescription { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Renders the delete confirmation page for the selected meal card.
    /// </summary>
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        return await LoadCardOrNotFoundAsync(cancellationToken).ConfigureAwait(false)
            ?? Page();
    }

    /// <summary>
    /// Deletes the selected meal card after explicit confirmation.
    /// </summary>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var loadCardResult = await TryLoadCardAsync(cancellationToken).ConfigureAwait(false);
        if (loadCardResult is not null)
        {
            return loadCardResult;
        }

        if (!ConfirmDelete)
        {
            return Page();
        }

        try
        {
            await _mealCardService.DeleteCardAsync(CardId!, cancellationToken).ConfigureAwait(false);
            StatusMessage = DeleteSucceededMessage;
            return RedirectToPage("/Cards/Index");
        }
        catch (KeyNotFoundException)
        {
            return CreateNotFoundResult();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
        {
            FailureMessage = DeleteFailedMessage;
            return Page();
        }
    }

    /// <summary>
    /// Returns the zh-TW label for the supplied meal type.
    /// </summary>
    public static string GetMealTypeLabel(MealType mealType) => global::CardPicker.Pages.IndexModel.GetMealTypeLabel(mealType);

    private async Task<IActionResult?> LoadCardOrNotFoundAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(CardId))
        {
            return CreateNotFoundResult();
        }

        Card = await _mealCardService.GetCardByIdAsync(CardId, cancellationToken).ConfigureAwait(false);
        if (Card is null)
        {
            return CreateNotFoundResult();
        }

        CacheCardSummary(Card);
        return null;
    }

    private NotFoundObjectResult CreateNotFoundResult() => NotFound("指定的餐點卡牌不存在。");

    private async Task<IActionResult?> TryLoadCardAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await LoadCardOrNotFoundAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
        {
            if (!TryRestoreCachedCard())
            {
                throw;
            }

            FailureMessage = DeleteFailedMessage;
            return Page();
        }
    }

    private void CacheCardSummary(MealCard card)
    {
        ArgumentNullException.ThrowIfNull(card);

        CachedCardId = card.Id;
        CachedCardName = card.Name;
        CachedMealType = card.MealType.ToString();
        CachedDescription = card.Description;
    }

    private bool TryRestoreCachedCard()
    {
        var cachedCardId = CachedCardId;
        var cachedCardName = CachedCardName;
        var cachedMealType = CachedMealType;
        var cachedDescription = CachedDescription;

        if (string.IsNullOrWhiteSpace(cachedCardId)
            || !string.Equals(cachedCardId, CardId, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(cachedCardName)
            || string.IsNullOrWhiteSpace(cachedMealType)
            || !Enum.TryParse<MealType>(cachedMealType, ignoreCase: true, out var mealType)
            || cachedDescription is null)
        {
            return false;
        }

        Card = new MealCard
        {
            Id = cachedCardId,
            Name = cachedCardName,
            MealType = mealType,
            Description = cachedDescription,
        };
        return true;
    }
}
