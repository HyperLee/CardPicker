using System.ComponentModel.DataAnnotations;
using CardPicker.Models;

namespace CardPicker.Pages.Cards;

/// <summary>
/// Represents the shared card form fields used by the create and edit pages.
/// </summary>
public sealed class CardFormInputModel
{
    /// <summary>
    /// Gets or sets the meal name entered by the user.
    /// </summary>
    [Display(Name = "餐點名稱")]
    [Required(ErrorMessage = "請輸入餐點名稱。")]
    [StringLength(MealCard.MaxNameLength, ErrorMessage = "餐點名稱不可超過 {1} 個字元。")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the meal period selected by the user.
    /// </summary>
    [Display(Name = "餐別")]
    [Required(ErrorMessage = "請選擇餐別。")]
    [EnumDataType(typeof(MealType), ErrorMessage = "請選擇有效的餐別。")]
    public MealType? MealType { get; set; }

    /// <summary>
    /// Gets or sets the meal description entered by the user.
    /// </summary>
    [Display(Name = "描述")]
    [DataType(DataType.MultilineText)]
    [Required(ErrorMessage = "請輸入餐點描述。")]
    [StringLength(MealCard.MaxDescriptionLength, ErrorMessage = "餐點描述不可超過 {1} 個字元。")]
    public string Description { get; set; } = string.Empty;
}
