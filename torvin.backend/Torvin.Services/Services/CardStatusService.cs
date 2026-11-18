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

public interface ICardStatusService
    : ICRUDService<CardStatusResponse, CardStatusSearchObject, CardStatusInsertRequest, CardStatusUpdateRequest>;

public sealed class CardStatusService(TorvinDbContext context, ReferenceDataCache cache, ILogger<CardStatusService> logger)
    : BaseCRUDService<CardStatusResponse, CardStatusSearchObject, CardStatus, CardStatusInsertRequest, CardStatusUpdateRequest>(
        context, cache, logger), ICardStatusService
{
    protected override string NotFoundMessage => "Status kartice nije pronađen.";

    protected override IQueryable<CardStatus> AddFilter(IQueryable<CardStatus> query, CardStatusSearchObject search)
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

    protected override bool HasFilters(CardStatusSearchObject search) =>
        base.HasFilters(search) || !string.IsNullOrWhiteSpace(search.Code);

    protected override async Task BeforeInsertAsync(CardStatusInsertRequest request, CancellationToken cancellationToken)
    {
        if (await Context.CardStatuses.AnyAsync(item => item.Code == request.Code, cancellationToken))
        {
            throw new BusinessException($"Status kartice s kodom {request.Code} već postoji.");
        }
    }

    protected override async Task BeforeDeleteAsync(CardStatus item, CancellationToken cancellationToken)
    {
        if (CardStatusCodes.All.Contains(item.Code))
        {
            throw new BusinessException($"Status kartice {item.Code} je sistemski i ne može se obrisati.");
        }

        ThrowIfInUse($"Status kartice {item.Code}", ("kartice", await Context.Cards.CountAsync(card => card.CardStatusId == item.Id, cancellationToken)));
    }
}
