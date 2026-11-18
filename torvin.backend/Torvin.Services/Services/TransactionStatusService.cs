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

public interface ITransactionStatusService
    : ICRUDService<TransactionStatusResponse, TransactionStatusSearchObject, TransactionStatusInsertRequest, TransactionStatusUpdateRequest>;

public sealed class TransactionStatusService(TorvinDbContext context, ReferenceDataCache cache, ILogger<TransactionStatusService> logger)
    : BaseCRUDService<TransactionStatusResponse, TransactionStatusSearchObject, TransactionStatus, TransactionStatusInsertRequest, TransactionStatusUpdateRequest>(
        context, cache, logger), ITransactionStatusService
{
    protected override string NotFoundMessage => "Status transakcije nije pronađen.";

    protected override IQueryable<TransactionStatus> AddFilter(IQueryable<TransactionStatus> query, TransactionStatusSearchObject search)
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

    protected override bool HasFilters(TransactionStatusSearchObject search) =>
        base.HasFilters(search) || !string.IsNullOrWhiteSpace(search.Code);

    protected override async Task BeforeInsertAsync(TransactionStatusInsertRequest request, CancellationToken cancellationToken)
    {
        if (await Context.TransactionStatuses.AnyAsync(item => item.Code == request.Code, cancellationToken))
        {
            throw new BusinessException($"Status transakcije s kodom {request.Code} već postoji.");
        }
    }

    protected override async Task BeforeDeleteAsync(TransactionStatus item, CancellationToken cancellationToken)
    {
        if (TransactionStatusCodes.All.Contains(item.Code))
        {
            throw new BusinessException($"Status transakcije {item.Code} je sistemski i ne može se obrisati.");
        }

        ThrowIfInUse($"Status transakcije {item.Code}", ("transakcije", await Context.Transactions
            .CountAsync(transaction => transaction.TransactionStatusId == item.Id, cancellationToken)));
    }
}
