using Mapster;
using Microsoft.EntityFrameworkCore;
using Torvin.Model.Exceptions;
using Torvin.Model.Responses;
using Torvin.Model.SearchObjects;
using Torvin.Services.Database;

namespace Torvin.Services.Services;

// Lista i detalj za svaki entitet: filter, brojanje, stranica i projekcija u DTO idu jednim upitom nad bazom
public abstract class BaseReadService<TModel, TSearch, TEntity>(TorvinDbContext context, ReferenceDataCache cache)
    : IReadService<TModel, TSearch>
    where TModel : class
    where TSearch : BaseSearchObject
    where TEntity : BaseEntity
{
    protected TorvinDbContext Context { get; } = context;

    protected ReferenceDataCache Cache { get; } = cache;

    protected static string EntityName => typeof(TEntity).Name;

    protected abstract string NotFoundMessage { get; }

    // U kešu je samo prva stranica bez pretrage i filtera, a to je dropdown. Tako keš ima najviše jedan unos po
    // veličini stranice, a korisnik ne može puniti memoriju API-ja nasumičnim pretragama
    public Task<PagedResult<TModel>> GetPagedAsync(TSearch search, CancellationToken cancellationToken = default) =>
        search.Page == 1 && !HasFilters(search)
            ? Cache.GetOrCreateAsync(EntityName, search.PageSize, () => LoadPageAsync(search, cancellationToken))
            : LoadPageAsync(search, cancellationToken);

    public async Task<TModel> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await Context.Set<TEntity>().AsNoTracking()
            .Where(entity => entity.Id == id)
            .ProjectToType<TModel>()
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException(NotFoundMessage);

    protected virtual IQueryable<TEntity> AddFilter(IQueryable<TEntity> query, TSearch search) => query;

    // Servis koji ima dodatni filter u AddFilter ga dodaje i ovdje, da filtrirana lista ne ide u keš
    protected virtual bool HasFilters(TSearch search) => !string.IsNullOrWhiteSpace(search.Fts);

    // Servis ograniči stranicu i sam, jer ga mogu zvati i drugi servisi, a ne samo controller s validacijom
    private async Task<PagedResult<TModel>> LoadPageAsync(TSearch search, CancellationToken cancellationToken)
    {
        var page = Math.Clamp(search.Page, 1, BaseSearchObject.MaxPage);
        var pageSize = Math.Clamp(search.PageSize, 1, BaseSearchObject.MaxPageSize);
        var query = AddFilter(Context.Set<TEntity>().AsNoTracking(), search);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(entity => entity.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ProjectToType<TModel>()
            .ToListAsync(cancellationToken);

        return new PagedResult<TModel>(items, totalCount, page, pageSize);
    }
}
