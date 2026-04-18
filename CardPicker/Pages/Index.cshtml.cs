using CardPicker.Models;
using CardPicker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CardPicker.Pages;

public class IndexModel : PageModel
{
    private readonly IMealDrawService _mealDrawService;

    public IndexModel(IMealDrawService mealDrawService)
    {
        ArgumentNullException.ThrowIfNull(mealDrawService);

        _mealDrawService = mealDrawService;
    }

    [BindProperty]
    public MealType? SelectedMealType { get; set; }

    public DrawResult DrawResult { get; private set; } = new();

    /// <summary>
    /// Renders the home page draw form.
    /// </summary>
    public void OnGet()
    {
    }

    /// <summary>
    /// Handles the home page draw submission.
    /// </summary>
    public async Task<IActionResult> OnPostDrawAsync(CancellationToken cancellationToken)
    {
        DrawResult = await _mealDrawService.DrawAsync(
                new DrawRequest { SelectedMealType = SelectedMealType },
                cancellationToken)
            .ConfigureAwait(false);

        return Page();
    }

    /// <summary>
    /// Returns the zh-TW label for the supplied meal type.
    /// </summary>
    public static string GetMealTypeLabel(MealType mealType) =>
        mealType switch
        {
            MealType.Breakfast => "早餐",
            MealType.Lunch => "午餐",
            MealType.Dinner => "晚餐",
            _ => throw new ArgumentOutOfRangeException(nameof(mealType), mealType, "Unsupported meal type."),
        };
}
