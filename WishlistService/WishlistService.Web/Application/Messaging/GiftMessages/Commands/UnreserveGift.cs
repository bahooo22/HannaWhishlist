using Calabonga.OperationResults;
using Calabonga.UnitOfWork;
using Mediator;
using WishlistService.Contracts.ViewModels;
using WishlistService.Domain.Base;
using WishlistService.Domain.Entities;

namespace WishlistService.Web.Application.Messaging.GiftMessages.Commands;

/// <summary>
/// Request: Gift unreservation
/// </summary>
public static class UnreserveGift
{
    public record Request(Guid GiftId, string UserId)
        : IRequest<Operation<GiftViewModel, string>>;

    public class Handler(IUnitOfWork unitOfWork)
        : IRequestHandler<Request, Operation<GiftViewModel, string>>
    {
        public async ValueTask<Operation<GiftViewModel, string>> Handle(Request request, CancellationToken _)
        {
            var repository = unitOfWork.GetRepository<Gift>();
            var entity = await repository.FindAsync(request.GiftId);

            if (entity == null)
            {
                return Operation.Error("Gift not found");
            }

            if (entity.Status != GiftStatus.Reserved || entity.ReservedBy != request.UserId)
            {
                return Operation.Error("You cannot unreserve this gift");
            }

            entity.Status = GiftStatus.Free;
            entity.ReservedBy = null;
            entity.ReservedAt = null;

            repository.Update(entity);
            await unitOfWork.SaveChangesAsync();

            if (!unitOfWork.Result.Ok)
            {
                return Operation.Error(unitOfWork.Result.Exception?.Message ?? AppData.Exceptions.SomethingWrong);
            }

            var mapped = entity.MapToViewModel();
            return mapped is not null
                ? Operation.Result(mapped)
                : Operation.Error(AppData.Exceptions.MappingException);
        }
    }
}
