using Calabonga.OperationResults;
using Calabonga.UnitOfWork;
using Mediator;
using WishlistService.Contracts.ViewModels;
using WishlistService.Domain.Base;
using WishlistService.Domain.Entities;

namespace WishlistService.Web.Application.Messaging.GiftMessages.Queries;

/// <summary>
/// Gift by Id query message
/// </summary>
public class GetGiftById
{
    /// <summary>
    /// Для запроса подарка по Id
    /// </summary>
    /// <param name="Id"></param>
    public record Request(Guid Id) : IRequest<Operation<GiftViewModel, string>>;

    /// <summary>
    /// Заголовок для обработки запроса подарка по Id
    /// </summary>
    /// <param name="unitOfWork"></param>
    public class Handler(IUnitOfWork unitOfWork) : IRequestHandler<Request, Operation<GiftViewModel, string>>
    {
        /// <inheritdoc />
        public async ValueTask<Operation<GiftViewModel, string>> Handle(Request request, CancellationToken cancellationToken = default!)
        {
            var repository = unitOfWork.GetRepository<Gift>();
            var entity = await repository.GetFirstOrDefaultAsync(
                predicate: x => x.Id == request.Id,
                trackingType: TrackingType.NoTracking);

            if (entity == null)
            {
                return Operation.Error($"Gift with id {request.Id} not found");
            }

            var mapped = entity.MapToViewModel();
            return mapped is not null
                ? Operation.Result(mapped)
                : Operation.Error(AppData.Exceptions.MappingException);
        }
    }
}
