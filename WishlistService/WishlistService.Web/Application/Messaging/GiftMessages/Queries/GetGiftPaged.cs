using Calabonga.OperationResults;
using Calabonga.PagedListCore;
using Calabonga.PredicatesBuilder;
using Calabonga.UnitOfWork;
using Mediator;
using System.Linq.Expressions;
using WishlistService.Contracts.ViewModels;
using WishlistService.Domain.Base;
using WishlistService.Domain.Entities;

namespace WishlistService.Web.Application.Messaging.GiftMessages.Queries;

/// <summary>
/// Request для получения постраничного списка подарков
/// </summary>
public class GetGiftPaged
{
    /// <summary>
    /// Запрос для получения постраничного списка подарков.
    /// Используется для передачи параметров пагинации (индекс страницы и размер)
    /// и необязательной строки поиска в обработчик запроса (например, MediatR Handler).
    /// </summary>
    /// <param name="PageIndex">
    /// **Индекс запрашиваемой страницы** (начиная с 0 или 1, в зависимости от внутренней реализации).
    /// </param>
    /// <param name="PageSize">
    /// **Максимальное количество элементов (подарков)**, которые должны быть включены в одну страницу.
    /// </param>
    /// <param name="Search">
    /// **Необязательная строка поиска** для фильтрации списка подарков по релевантным полям (например, по названию). Может быть <c>null</c>.
    /// </param>
    public record Request(int PageIndex, int PageSize, string? Search)
        : IRequest<Operation<IPagedList<GiftViewModel>, string>>;

    public class Handler(IUnitOfWork unitOfWork) : IRequestHandler<Request, Operation<IPagedList<GiftViewModel>, string>>
    {
        /// <inheritdoc />
        public async ValueTask<Operation<IPagedList<GiftViewModel>, string>> Handle(Request request, CancellationToken _)
        {
            var predicate = GetPredicate(request.Search);

            var pagedList = await unitOfWork.GetRepository<Gift>()
                .GetPagedListAsync(
                    predicate: predicate,
                    pageIndex: request.PageIndex,
                    pageSize: request.PageSize,
                    orderBy: x => x.OrderBy(g => g.Title),
                    trackingType: TrackingType.NoTracking);

            if (pagedList.PageIndex > pagedList.TotalPages)
            {
                pagedList = await unitOfWork.GetRepository<Gift>()
                    .GetPagedListAsync(
                        pageIndex: 0,
                        pageSize: request.PageSize,
                        trackingType: TrackingType.NoTracking);
            }

            var mapped = PagedList.From(pagedList, items => items.Select(item => item.MapToViewModel()!));
            return mapped is not null
                ? Operation.Result(mapped)
                : Operation.Error(AppData.Exceptions.MappingException);
        }

        private Expression<Func<Gift, bool>> GetPredicate(string? search)
        {
            var predicate = PredicateBuilder.True<Gift>();
            if (string.IsNullOrWhiteSpace(search))
            {
                return predicate;
            }

            var searchLower = search.ToLower();

            // Поиск по основным полям подарка
            predicate = predicate.And(x => x.Title.ToLower().Contains(searchLower));
            predicate = predicate.Or(x => x.Link != null && x.Link.ToLower().Contains(searchLower));

            // Поиск по статусу (например, "free", "reserved")
            predicate = predicate.Or(x => x.Status.ToString().ToLower().Contains(searchLower));

            // Поиск по полям пользователя
            predicate = predicate.Or(x => x.ReservedById != null && x.ReservedById.ToLower().Contains(searchLower));
            predicate = predicate.Or(x => x.ReservedByNickname != null && x.ReservedByNickname.ToLower().Contains(searchLower));
            predicate = predicate.Or(x => x.ReservedByFirstName != null && x.ReservedByFirstName.ToLower().Contains(searchLower));
            predicate = predicate.Or(x => x.ReservedByLastName != null && x.ReservedByLastName.ToLower().Contains(searchLower));

            return predicate;
        }
    }
}
