using System.ComponentModel.DataAnnotations;
using WishlistService.Domain.Base;

namespace WishlistService.Contracts.ViewModels;

/// <summary>
/// ViewModel для подарка
/// </summary>
public class GiftViewModel : ViewModelBase
{
    public string Title { get; set; } = string.Empty;
    [Url] public string? Link { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ReservedById { get; set; } // Для хранения уникального ID пользователя (например, из внешней системы)
    public string? ReservedByNickname { get; set; } // Для хранения @username (технический ID)
    public string? ReservedByFirstName { get; set; }
    public string? ReservedByLastName { get; set; }
    public DateTime? ReservedAt { get; set; }
}
