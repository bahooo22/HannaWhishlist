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
                ReservedById = source.ReservedById,
                ReservedByNickname = source.ReservedByNickname,
                ReservedByFirstName = source.ReservedByFirstName,
                ReservedByLastName = source.ReservedByLastName,
                ReservedAt = source.ReservedAt
            };

    public static Gift? MapToGift(this GiftCreateViewModel? source) => source == null
            ? null
            : new Gift
            {
                Title = source.Title,
                Link = source.Link
            };

    public static void MapUpdatesFrom(this Gift source, GiftUpdateViewModel updateViewModel)
    {
        if (source is null || updateViewModel is null)
        {
            return;
        }

        source.Title = updateViewModel.Title ?? source.Title;
        source.Link = updateViewModel.Link ?? source.Link;

        if (updateViewModel.Status != null)
        {
            source.Status = Enum.TryParse<GiftStatus>(updateViewModel.Status, out var status)
                ? status
                : source.Status;
        }

        // Обновляем поля пользователя только если они предоставлены
        source.ReservedById = updateViewModel.ReservedById ?? source.ReservedById;
        source.ReservedByNickname = updateViewModel.ReservedByNickname ?? source.ReservedByNickname;
        source.ReservedByFirstName = updateViewModel.ReservedByFirstName ?? source.ReservedByFirstName;
        source.ReservedByLastName = updateViewModel.ReservedByLastName ?? source.ReservedByLastName;
        source.ReservedAt = updateViewModel.ReservedAt ?? source.ReservedAt;
    }
}
