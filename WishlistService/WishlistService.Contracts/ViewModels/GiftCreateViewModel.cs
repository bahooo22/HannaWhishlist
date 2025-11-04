using System.ComponentModel.DataAnnotations;
using WishlistService.Domain.Base;

namespace WishlistService.Contracts.ViewModels;

/// <summary>
/// ViewModel для создания подарка
/// </summary>
public class GiftCreateViewModel : IViewModel
{
    public string Title { get; set; } = default!;
    [Url] public string? Link { get; set; }
}
