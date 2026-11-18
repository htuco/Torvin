using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Torvin.Model.Exceptions;
using Torvin.Model.Requests;
using Torvin.Model.Responses;
using Torvin.Model.SearchObjects;
using Torvin.Services.Database;
using Torvin.Services.Database.Entities;

namespace Torvin.Services.Services;

public interface ITransactionCategoryService
    : ICRUDService<TransactionCategoryResponse, TransactionCategorySearchObject, TransactionCategoryRequest,
        TransactionCategoryRequest>;

public sealed class TransactionCategoryService(
    TorvinDbContext context,
    ReferenceDataCache cache,
    ILogger<TransactionCategoryService> logger)
    : BaseCRUDService<TransactionCategoryResponse, TransactionCategorySearchObject, TransactionCategory,
        TransactionCategoryRequest, TransactionCategoryRequest>(context, cache, logger), ITransactionCategoryService
{
    protected override string NotFoundMessage => "Kategorija nije pronađena.";

    protected override IQueryable<TransactionCategory> AddFilter(
        IQueryable<TransactionCategory> query, TransactionCategorySearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.Fts))
        {
            var fts = search.Fts.Trim();
            query = query.Where(category => category.Name.Contains(fts));
        }

        return query;
    }

    protected override Task BeforeInsertAsync(TransactionCategoryRequest request, CancellationToken cancellationToken) =>
        EnsureNameIsFreeAsync(request.Name, 0, cancellationToken);

    protected override Task BeforeUpdateAsync(int id, TransactionCategoryRequest request,
        CancellationToken cancellationToken) =>
        EnsureNameIsFreeAsync(request.Name, id, cancellationToken);

    // Budžeti se brišu mekano, ali red ostaje i FK ga i dalje vidi, pa se broje i obrisani
    protected override async Task BeforeDeleteAsync(TransactionCategory category, CancellationToken cancellationToken) =>
        ThrowIfInUse($"Kategorija {category.Name}",
            ("transakcije", await Context.Transactions
                .CountAsync(transaction => transaction.TransactionCategoryId == category.Id, cancellationToken)),
            ("budžeti", await Context.Budgets.IgnoreQueryFilters()
                .CountAsync(budget => budget.TransactionCategoryId == category.Id, cancellationToken)));

    private async Task EnsureNameIsFreeAsync(string name, int ownId, CancellationToken cancellationToken)
    {
        if (await Context.TransactionCategories.AnyAsync(
                category => category.Name == name && category.Id != ownId, cancellationToken))
        {
            throw new BusinessException($"Kategorija s nazivom {name} već postoji.");
        }
    }
}
