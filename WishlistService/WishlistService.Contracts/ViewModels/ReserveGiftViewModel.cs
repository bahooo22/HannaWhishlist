using System.ComponentModel.DataAnnotations;

namespace WishlistService.Contracts.ViewModels;

public class ReserveGiftViewModel
{
    [Required]
    public string ReservedById { get; set; } = string.Empty;      // Telegram User ID

    [Required]
    public string ReservedByNickname { get; set; } = string.Empty; // @username

    [Required]
    public string ReservedByFirstName { get; set; } = string.Empty;

    public string? ReservedByLastName { get; set; }
}
