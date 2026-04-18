using CardPicker.Models;
using CardPicker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CardPicker.Pages.Cards;

public sealed class IndexModel : PageModel
{
    private const string StatusMessageKey = "StatusMessage";

    private readonly IMealCardService _mealCardService;

    public IndexModel(IMealCardService mealCardService)
    {
        ArgumentNullException.ThrowIfNull(mealCardService);

        _mealCardService = mealCardService;
    }

    [BindProperty(SupportsGet = true, Name = "keyword")]
    public string? Keyword { get; set; }

    [BindProperty(SupportsGet = true, Name = "mealType")]
    public MealType? MealType { get; set; }

    [BindProperty(SupportsGet = true, Name = "id")]
    public string? SelectedCardId { get; set; }

    [TempData(Key = StatusMessageKey)]
    public string? StatusMessage { get; set; }

    public IReadOnlyList<MealCard> Cards { get; private set; } = Array.Empty<MealCard>();

    public MealCard? SelectedCard { get; private set; }

    public bool HasFilters => SearchCriteria.HasFilters;

    public bool HasCards => Cards.Count > 0;

    private CardSearchCriteria SearchCriteria =>
        new()
        {
            Keyword = Keyword,
            MealType = MealType,
        };

    /// <summary>
    /// Renders the card library with optional search filters and a selected card detail.
    /// </summary>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Cards = await _mealCardService.GetCardsAsync(
                HasFilters ? SearchCriteria : null,
                cancellationToken)
            .ConfigureAwait(false);

        if (!Guid.TryParse(SelectedCardId, out _))
        {
            return;
        }

        SelectedCard = await _mealCardService.GetCardByIdAsync(SelectedCardId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the zh-TW label for the supplied meal type.
    /// </summary>
    public static string GetMealTypeLabel(MealType mealType) => global::CardPicker.Pages.IndexModel.GetMealTypeLabel(mealType);
}
