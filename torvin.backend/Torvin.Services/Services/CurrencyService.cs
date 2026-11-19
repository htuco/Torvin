using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Torvin.Model.Exceptions;
using Torvin.Model.Requests;
using Torvin.Model.Responses;
using Torvin.Model.SearchObjects;
using Torvin.Services.Database;
using Torvin.Services.Database.Entities;

namespace Torvin.Services.Services;

public interface ICurrencyService
    : ICRUDService<CurrencyResponse, CurrencySearchObject, CurrencyInsertRequest, CurrencyUpdateRequest>;

public sealed class CurrencyService(TorvinDbContext context, ReferenceDataCache cache, ILogger<CurrencyService> logger)
    : BaseCRUDService<CurrencyResponse, CurrencySearchObject, Currency, CurrencyInsertRequest, CurrencyUpdateRequest>(
        context, cache, logger), ICurrencyService
{
    protected override string NotFoundMessage => "Valuta nije pronađena.";

    protected override IQueryable<Currency> AddFilter(IQueryable<Currency> query, CurrencySearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.Fts))
        {
            var fts = search.Fts.Trim();
            query = query.Where(currency => currency.Name.Contains(fts) || currency.Code.Contains(fts));
        }

        if (!string.IsNullOrWhiteSpace(search.Code))
        {
            query = query.Where(currency => currency.Code == search.Code);
        }

        return query;
    }

    protected override bool HasFilters(CurrencySearchObject search) =>
        base.HasFilters(search) || !string.IsNullOrWhiteSpace(search.Code);

    protected override async Task BeforeInsertAsync(CurrencyInsertRequest request, CancellationToken cancellationToken)
    {
        if (await Context.Currencies.AnyAsync(currency => currency.Code == request.Code, cancellationToken))
        {
            throw new BusinessException($"Valuta s kodom {request.Code} već postoji.");
        }
    }

    // Budžeti i ciljevi štednje se brišu mekano, ali red ostaje i FK ga i dalje vidi, pa se broje i obrisani
    protected override async Task BeforeDeleteAsync(Currency currency, CancellationToken cancellationToken) =>
        ThrowIfInUse($"Valuta {currency.Code}",
            ("kartice", await Context.Cards.CountAsync(card => card.CurrencyId == currency.Id, cancellationToken)),
            ("transakcije", await Context.Transactions
                .CountAsync(transaction => transaction.CurrencyId == currency.Id, cancellationToken)),
            ("budžeti", await Context.Budgets.IgnoreQueryFilters()
                .CountAsync(budget => budget.CurrencyId == currency.Id, cancellationToken)),
            ("zahtjevi za novac", await Context.MoneyRequests
                .CountAsync(request => request.CurrencyId == currency.Id, cancellationToken)),
            ("ciljevi štednje", await Context.SavingsGoals.IgnoreQueryFilters()
                .CountAsync(goal => goal.CurrencyId == currency.Id, cancellationToken)));
}
