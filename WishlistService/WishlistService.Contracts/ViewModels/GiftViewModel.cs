using System.ComponentModel.DataAnnotations;
using WishlistService.Domain.Base;

namespace WishlistService.Contracts.ViewModels;

/// <summary>
/// ViewModel для подарка
/// </summary>
public class GiftViewModel : ViewModelBase
{
    public string Title { get; set; } = default!;
    [Url] public string? Link { get; set; }
    public string Status { get; set; } = default!;
    public string? ReservedBy { get; set; }
    public DateTime? ReservedAt { get; set; }
}
