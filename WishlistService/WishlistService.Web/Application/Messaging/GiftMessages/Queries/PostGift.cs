using Calabonga.OperationResults;
using Calabonga.UnitOfWork;
using Mediator;
using WishlistService.Contracts.ViewModels;
using WishlistService.Domain.Base;
using WishlistService.Domain.Entities;

namespace WishlistService.Web.Application.Messaging.GiftMessages.Queries;

/// <summary>
/// Request: создание Gift
/// </summary>
public static class PostGift
{
    public record Request(GiftCreateViewModel Model)
        : IRequest<Operation<GiftViewModel, string>>;

    public class Handler(IUnitOfWork unitOfWork, ILogger<Handler> logger)
        : IRequestHandler<Request, Operation<GiftViewModel, string>>
    {
        public async ValueTask<Operation<GiftViewModel, string>> Handle(Request request, CancellationToken _)
        {
            logger.LogDebug("Creating new Gift");

            var entity = request.Model.MapToGift();
            if (entity == null)
            {
                logger.LogError("Mapper not configured correctly or something went wrong");
                return Operation.Error(AppData.Exceptions.MappingException);
            }

            await unitOfWork.GetRepository<Gift>().InsertAsync(entity);
            await unitOfWork.SaveChangesAsync();

            if (unitOfWork.Result.Ok)
            {
                var mapped = entity.MapToViewModel();
                return mapped is not null
                    ? Operation.Result(mapped)
                    : Operation.Error(AppData.Exceptions.MappingException);
            }

            var errorMessage = unitOfWork.Result.Exception?.Message ?? "Something went wrong";
            logger.LogError(errorMessage);
            return Operation.Error(errorMessage);
        }
    }
}
