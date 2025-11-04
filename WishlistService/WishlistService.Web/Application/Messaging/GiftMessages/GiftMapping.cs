using WishlistService.Contracts.ViewModels;
using WishlistService.Domain.Entities;

namespace WishlistService.Web.Application.Messaging.GiftMessages;

/// <summary>
/// Маппинг Gift <-> ViewModels
/// </summary>
public static class GiftMapping
{
    public static GiftViewModel? MapToViewModel(this Gift? source) => source == null
            ? null
            : new GiftViewModel
            {
                Id = source.Id,
                Title = source.Title,
                Link = source.Link,
                Status = source.Status.ToString(),
                ReservedBy = source.ReservedBy,
                ReservedAt = source.ReservedAt
            };

    public static Gift? MapToGift(this GiftCreateViewModel? source) => source == null
            ? null
            : new Gift
            {
                Title = source.Title,
                Link = source.Link
            };

    public static void MapUpdatesFrom(this Gift? source, GiftUpdateViewModel? updateViewModel)
    {
        if (source is null || updateViewModel is null)
        {
            return;
        }

        source.Title = updateViewModel.Title;
        source.Link = updateViewModel.Link;
        source.Status = Enum.TryParse<GiftStatus>(updateViewModel.Status, out var status)
            ? status
            : source.Status;
        source.ReservedBy = updateViewModel.ReservedBy;
        source.ReservedAt = updateViewModel.ReservedAt;
    }
}
