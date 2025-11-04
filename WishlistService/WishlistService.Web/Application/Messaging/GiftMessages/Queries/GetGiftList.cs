using Calabonga.OperationResults;
using Calabonga.UnitOfWork;
using Mediator;
using WishlistService.Contracts.ViewModels;
using WishlistService.Domain.Base;
using WishlistService.Domain.Entities;

namespace WishlistService.Web.Application.Messaging.GiftMessages.Queries;

/// <summary>
/// Request to get list of gifts
/// </summary>
public class GetGiftList
{
    /// <summary>
    /// Запрос для получения списка подарков
    /// </summary>
    public record Request() : IRequest<Operation<List<GiftViewModel>, string>>;

    public class Handler(IUnitOfWork unitOfWork)
        : IRequestHandler<Request, Operation<List<GiftViewModel>, string>>
    {
        /// <inheritdoc />
        public async ValueTask<Operation<List<GiftViewModel>, string>> Handle(Request request, CancellationToken _)
        {
            var repository = unitOfWork.GetRepository<Gift>();
            var entities = await repository.GetAllAsync(
                trackingType: TrackingType.NoTracking);

            var mapped = entities.Select(x => x.MapToViewModel()!).ToList();
            return mapped is not null
                ? Operation.Result(mapped)
                : Operation.Error(AppData.Exceptions.MappingException);
        }
    }
}
