using Calabonga.Microservices.Core;
using Calabonga.OperationResults;
using Calabonga.UnitOfWork;
using Mediator;
using WishlistService.Contracts.ViewModels;
using WishlistService.Domain.Base;
using WishlistService.Domain.Entities;

namespace WishlistService.Web.Application.Messaging.GiftMessages.Queries;

/// <summary>
/// Request: Gift update
/// </summary>
public static class UpdateGift
{
    public record Request(Guid Id, GiftUpdateViewModel Model)
        : IRequest<Operation<GiftViewModel, string>>;

    public class Handler(IUnitOfWork unitOfWork)
        : IRequestHandler<Request, Operation<GiftViewModel, string>>
    {
        public async ValueTask<Operation<GiftViewModel, string>> Handle(Request request, CancellationToken _)
        {
            var repository = unitOfWork.GetRepository<Gift>();
            var entity = await repository.GetFirstOrDefaultAsync(predicate: x => x.Id == request.Id, trackingType: TrackingType.Tracking);

            if (entity == null)
            {
                return Operation.Error(AppContracts.Exceptions.NotFoundException);
            }

            entity.MapUpdatesFrom(request.Model);

            repository.Update(entity);
            await unitOfWork.SaveChangesAsync();

            if (unitOfWork.Result.Ok)
            {
                var mapped = entity.MapToViewModel();
                return mapped is not null
                    ? Operation.Result(mapped)
                    : Operation.Error(AppData.Exceptions.MappingException);
            }

            var errorMessage = unitOfWork.Result.Exception?.Message ?? "Something went wrong";
            return Operation.Error(errorMessage);
        }
    }
}
