using Mapster;
using Microsoft.EntityFrameworkCore;
using Torvin.Model.Responses;
using Torvin.Model.SearchObjects;

namespace Torvin.Services.Services;

// Stranica liste jednim upitom nad bazom: brojanje, preskakanje, uzimanje i projekcija u DTO.
// Upit dolazi već filtriran i sortiran, jer sort zavisi od liste (šifarnik po Id-u, kartice od najnovije)
public static class PagingExtensions
{
    public static Task<PagedResult<TModel>> ToPagedResultAsync<TEntity, TModel>(this IQueryable<TEntity> query,
        BaseSearchObject search, CancellationToken cancellationToken = default) =>
        query.ProjectToType<TModel>().ToPagedResultAsync(search, cancellationToken);

    // Za upit koji je već projektovan u DTO (npr. polja zavise od toga ko gleda listu)
    // Stranica se ograniči i ovdje, jer servise mogu zvati i drugi servisi, a ne samo controller s validacijom
    public static async Task<PagedResult<TModel>> ToPagedResultAsync<TModel>(this IQueryable<TModel> query,
        BaseSearchObject search, CancellationToken cancellationToken = default)
    {
        var page = Math.Clamp(search.Page, 1, BaseSearchObject.MaxPage);
        var pageSize = Math.Clamp(search.PageSize, 1, BaseSearchObject.MaxPageSize);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TModel>(items, totalCount, page, pageSize);
    }
}
