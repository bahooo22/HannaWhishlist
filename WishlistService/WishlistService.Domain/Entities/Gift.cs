using System.ComponentModel.DataAnnotations;

namespace WishlistService.Domain.Entities;

public enum GiftStatus { Free, Reserved }

public class Gift : Auditable
{
    public string Title { get; set; } = string.Empty;
    [Url] public string? Link { get; set; }
    public GiftStatus Status { get; set; } = GiftStatus.Free;
    public string? ReservedById { get; set; } // Для хранения уникального ID пользователя (например, из внешней системы)
    public string? ReservedByNickname { get; set; } // Для хранения @username (технический ID)
    public string? ReservedByFirstName { get; set; }
    public string? ReservedByLastName { get; set; }
    public DateTime? ReservedAt { get; set; } = DateTime.UtcNow;
}
