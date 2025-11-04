using Calabonga.OperationResults;
using Calabonga.UnitOfWork;
using Mediator;
using WishlistService.Contracts.ViewModels;
using WishlistService.Domain.Base;
using WishlistService.Domain.Entities;

namespace WishlistService.Web.Application.Messaging.GiftMessages.Commands;

/// <summary>
/// Request: Gift reservation
/// </summary>
public static class ReserveGift
{
    public record Request(
        Guid GiftId,
        string UserId,             // AuthServer ID
        string? ReservedById,      // Новый: Telegram User ID (числовой, если нужен)
        string? ReservedByNickname, // @username (для проверки прав)
        string? ReservedByFirstName,
        string? ReservedByLastName)
        : IRequest<Operation<GiftViewModel, string>>;

    public class Handler(IUnitOfWork unitOfWork)
        : IRequestHandler<Request, Operation<GiftViewModel, string>>
    {
        public async ValueTask<Operation<GiftViewModel, string>> Handle(Request request, CancellationToken _)
        {
            var repository = unitOfWork.GetRepository<Gift>();
            var entity = await repository.FindAsync(request.GiftId);

            if (entity is null)
            {
                return Operation.Error($"Gift with Id: {request.GiftId} not found");
            }

            if (entity.Status == GiftStatus.Reserved && entity.ReservedById != request.UserId)
            {
                return Operation.Error("Gift already reserved by another user");
            }

            entity.Status = GiftStatus.Reserved;
            entity.ReservedById = request.ReservedById;
            entity.ReservedByNickname = request.ReservedByNickname;
            entity.ReservedByFirstName = request.ReservedByFirstName;
            entity.ReservedByLastName = request.ReservedByLastName;
            entity.ReservedAt = DateTime.UtcNow;

            repository.Update(entity);
            await unitOfWork.SaveChangesAsync();

            var mapped = entity.MapToViewModel();
            return mapped is not null
                ? Operation.Result(mapped)
                : Operation.Error(AppData.Exceptions.MappingException);
        }
    }
}
