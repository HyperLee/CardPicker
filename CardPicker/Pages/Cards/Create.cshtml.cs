using System.IO;
using CardPicker.Models;
using CardPicker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CardPicker.Pages.Cards;

public sealed class CreateModel : PageModel
{
    private const string DuplicateMealCardMessage = "相同內容的卡牌已存在，請勿重複新增。";
    private const string GenericSaveFailureMessage = "儲存餐點卡牌失敗，請稍後再試。";
    private const string CreateSucceededMessage = "已成功新增餐點卡牌。";

    private readonly IMealCardService _mealCardService;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(IMealCardService mealCardService, ILogger<CreateModel> logger)
    {
        ArgumentNullException.ThrowIfNull(mealCardService);
        ArgumentNullException.ThrowIfNull(logger);

        _mealCardService = mealCardService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the shared input model for the create form.
    /// </summary>
    [BindProperty]
    public CardFormInputModel Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Renders the create card page.
    /// </summary>
    public void OnGet()
    {
    }

    /// <summary>
    /// Creates a meal card and redirects back to the card library when successful.
    /// </summary>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!Input.MealType.HasValue)
        {
            return Page();
        }

        try
        {
            await _mealCardService.CreateCardAsync(
                Input.Name,
                Input.MealType.Value,
                Input.Description,
                cancellationToken);

            StatusMessage = CreateSucceededMessage;
            return RedirectToPage("/Cards/Index");
        }
        catch (InvalidDataException ex) when (string.Equals(ex.Message, DuplicateMealCardMessage, StringComparison.Ordinal))
        {
            _logger.LogWarning(ex, "Meal card creation failed because the card content already exists.");
            ModelState.AddModelError(string.Empty, DuplicateMealCardMessage);
            return Page();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
        {
            _logger.LogError(ex, "Meal card creation failed while persisting the new card.");
            ModelState.AddModelError(string.Empty, GenericSaveFailureMessage);
            return Page();
        }
    }
}
