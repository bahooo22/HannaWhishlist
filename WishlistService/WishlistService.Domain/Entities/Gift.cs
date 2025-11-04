using System.ComponentModel.DataAnnotations;

namespace WishlistService.Domain.Entities;

public enum GiftStatus { Free, Reserved }

public class Gift : Auditable
{
    public string Title { get; set; } = default!;
    [Url] public string? Link { get; set; }
    public GiftStatus Status { get; set; } = GiftStatus.Free;
    public string? ReservedBy { get; set; }
    public DateTime? ReservedAt { get; set; } = DateTime.UtcNow;
}

