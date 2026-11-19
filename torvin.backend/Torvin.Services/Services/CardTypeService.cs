using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Torvin.Model.Exceptions;
using Torvin.Model.Requests;
using Torvin.Model.Responses;
using Torvin.Model.SearchObjects;
using Torvin.Services.Database;
using Torvin.Services.Database.Entities;

namespace Torvin.Services.Services;

public interface ICardTypeService : ICRUDService<CardTypeResponse, CardTypeSearchObject, CardTypeRequest, CardTypeRequest>;

public sealed class CardTypeService(TorvinDbContext context, ReferenceDataCache cache, ILogger<CardTypeService> logger)
    : BaseCRUDService<CardTypeResponse, CardTypeSearchObject, CardType, CardTypeRequest, CardTypeRequest>(
        context, cache, logger), ICardTypeService
{
    protected override string NotFoundMessage => "Tip kartice nije pronađen.";

    protected override IQueryable<CardType> AddFilter(IQueryable<CardType> query, CardTypeSearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.Fts))
        {
            var fts = search.Fts.Trim();
            query = query.Where(type => type.Name.Contains(fts));
        }

        return query;
    }

    protected override Task BeforeInsertAsync(CardTypeRequest request, CancellationToken cancellationToken) =>
        EnsureNameIsFreeAsync(request.Name, 0, cancellationToken);

    protected override Task BeforeUpdateAsync(int id, CardTypeRequest request, CancellationToken cancellationToken) =>
        EnsureNameIsFreeAsync(request.Name, id, cancellationToken);

    protected override async Task BeforeDeleteAsync(CardType type, CancellationToken cancellationToken) =>
        ThrowIfInUse($"Tip kartice {type.Name}",
            ("kartice", await Context.Cards.CountAsync(card => card.CardTypeId == type.Id, cancellationToken)));

    private async Task EnsureNameIsFreeAsync(string name, int ownId, CancellationToken cancellationToken)
    {
        if (await Context.CardTypes.AnyAsync(type => type.Name == name && type.Id != ownId, cancellationToken))
        {
            throw new BusinessException($"Tip kartice s nazivom {name} već postoji.");
        }
    }
}
