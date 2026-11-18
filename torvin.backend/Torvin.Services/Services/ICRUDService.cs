using Torvin.Model.SearchObjects;

namespace Torvin.Services.Services;

public interface ICRUDService<TModel, in TSearch, in TInsert, in TUpdate> : IReadService<TModel, TSearch>
    where TSearch : BaseSearchObject
{
    Task<TModel> InsertAsync(TInsert request, CancellationToken cancellationToken = default);

    Task<TModel> UpdateAsync(int id, TUpdate request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
