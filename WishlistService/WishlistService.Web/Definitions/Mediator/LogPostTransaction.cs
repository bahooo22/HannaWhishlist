using Calabonga.OperationResults;
using Calabonga.UnitOfWork;
using Mediator;
using WishlistService.Contracts.ViewModels;
using WishlistService.Web.Definitions.Mediator.Base;

namespace WishlistService.Web.Definitions.Mediator;

public class LogPostTransactionBehavior : TransactionBehavior<IRequest<Operation<GiftViewModel>>, Operation<GiftViewModel>>
{
    public LogPostTransactionBehavior(IUnitOfWork unitOfWork) : base(unitOfWork) { }
}
