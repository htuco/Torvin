using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Torvin.Model.Constants;
using Torvin.Model.Exceptions;
using Torvin.Model.Requests;
using Torvin.Model.Responses;
using Torvin.Model.SearchObjects;
using Torvin.Services.Database;
using Torvin.Services.Database.Entities;

namespace Torvin.Services.Services;

public interface ITransactionTypeService
    : ICRUDService<TransactionTypeResponse, TransactionTypeSearchObject, TransactionTypeInsertRequest, TransactionTypeUpdateRequest>;

public sealed class TransactionTypeService(TorvinDbContext context, ReferenceDataCache cache, ILogger<TransactionTypeService> logger)
    : BaseCRUDService<TransactionTypeResponse, TransactionTypeSearchObject, TransactionType, TransactionTypeInsertRequest, TransactionTypeUpdateRequest>(
        context, cache, logger), ITransactionTypeService
{
    protected override string NotFoundMessage => "Tip transakcije nije pronađen.";

    protected override IQueryable<TransactionType> AddFilter(IQueryable<TransactionType> query, TransactionTypeSearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.Fts))
        {
            var fts = search.Fts.Trim();
            query = query.Where(item => item.Name.Contains(fts) || item.Code.Contains(fts));
        }

        if (!string.IsNullOrWhiteSpace(search.Code))
        {
            query = query.Where(item => item.Code == search.Code);
        }

        return query;
    }

    protected override bool HasFilters(TransactionTypeSearchObject search) =>
        base.HasFilters(search) || !string.IsNullOrWhiteSpace(search.Code);

    protected override async Task BeforeInsertAsync(TransactionTypeInsertRequest request, CancellationToken cancellationToken)
    {
        if (await Context.TransactionTypes.AnyAsync(item => item.Code == request.Code, cancellationToken))
        {
            throw new BusinessException($"Tip transakcije s kodom {request.Code} već postoji.");
        }
    }

    protected override async Task BeforeDeleteAsync(TransactionType item, CancellationToken cancellationToken)
    {
        if (TransactionTypeCodes.All.Contains(item.Code))
        {
            throw new BusinessException($"Tip transakcije {item.Code} je sistemski i ne može se obrisati.");
        }

        ThrowIfInUse($"Tip transakcije {item.Code}", ("transakcije", await Context.Transactions
            .CountAsync(transaction => transaction.TransactionTypeId == item.Id, cancellationToken)));
    }
}
