using Calabonga.OperationResults;
using Calabonga.UnitOfWork;
using Mediator;
using WishlistService.Contracts.ViewModels;
using WishlistService.Domain.Base;
using WishlistService.Domain.Entities;

namespace WishlistService.Web.Application.Messaging.GiftMessages.Queries;

/// <summary>
/// Request: Gift delete
/// </summary>
public static class DeleteGift
{
    public record Request(Guid Id) : IRequest<Operation<GiftViewModel, string>>;

    public class Handler(IUnitOfWork unitOfWork)
        : IRequestHandler<Request, Operation<GiftViewModel, string>>
    {
        public async ValueTask<Operation<GiftViewModel, string>> Handle(Request request, CancellationToken _)
        {
            var repository = unitOfWork.GetRepository<Gift>();
            var entity = await repository.FindAsync(request.Id);

            if (entity == null)
            {
                return Operation.Error("Gift not found");
            }

            repository.Delete(entity);
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
