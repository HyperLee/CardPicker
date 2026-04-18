using System.IO;
using CardPicker.Models;
using CardPicker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CardPicker.Pages.Cards;

public sealed class EditModel : PageModel
{
    private const string CardNotFoundMessage = "指定的餐點卡牌不存在。";
    private const string GenericEditFailureMessage = "更新餐點卡牌失敗，請稍後再試。";
    private const string EditSucceededMessage = "已成功更新餐點卡牌。";

    private readonly IMealCardService _mealCardService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(IMealCardService mealCardService, ILogger<EditModel> logger)
    {
        ArgumentNullException.ThrowIfNull(mealCardService);
        ArgumentNullException.ThrowIfNull(logger);

        _mealCardService = mealCardService;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true, Name = "id")]
    public string? CardId { get; set; }

    [BindProperty]
    public CardFormInputModel Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Renders the edit page with the existing meal card values.
    /// </summary>
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(CardId))
        {
            return CreateCardNotFoundResult();
        }

        var card = await GetCardAsync(CardId, cancellationToken).ConfigureAwait(false);
        if (card is null)
        {
            return CreateCardNotFoundResult();
        }

        CardId = card.Id;
        PopulateInput(card);

        return Page();
    }

    /// <summary>
    /// Handles the edit form submission and persists the updated meal card.
    /// </summary>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(CardId))
        {
            return CreateCardNotFoundResult();
        }

        if (Input.MealType is null)
        {
            ModelState.AddModelError("Input.MealType", "請選擇餐別。");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var mealType = Input.MealType.GetValueOrDefault();

        try
        {
            var updatedCard = await _mealCardService.EditCardAsync(
                    CardId,
                    Input.Name,
                    mealType,
                    Input.Description,
                    cancellationToken)
                .ConfigureAwait(false);

            CardId = updatedCard.Id;
            StatusMessage = EditSucceededMessage;
            return RedirectToPage("/Cards/Index");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Meal card {CardId} was not found during edit.", CardId);
            return CreateCardNotFoundResult();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Meal card edit validation failed for {CardId}.", CardId);
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
        catch (InvalidDataException ex)
        {
            _logger.LogWarning(ex, "Meal card edit content validation failed for {CardId}.", CardId);
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            _logger.LogError(ex, "Failed to persist edited meal card {CardId}.", CardId);
            ModelState.AddModelError(string.Empty, GenericEditFailureMessage);
            return Page();
        }
    }

    private async Task<MealCard?> GetCardAsync(string cardId, CancellationToken cancellationToken)
    {
        try
        {
            return await _mealCardService.GetCardByIdAsync(cardId, cancellationToken).ConfigureAwait(false);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Meal card ID {CardId} is invalid for edit page access.", cardId);
            return null;
        }
    }

    private void PopulateInput(MealCard card)
    {
        ArgumentNullException.ThrowIfNull(card);

        Input = new CardFormInputModel
        {
            Name = card.Name,
            MealType = card.MealType,
            Description = card.Description,
        };
    }

    private ContentResult CreateCardNotFoundResult() =>
        new()
        {
            StatusCode = StatusCodes.Status404NotFound,
            Content = CardNotFoundMessage,
            ContentType = "text/plain; charset=utf-8",
        };
}
