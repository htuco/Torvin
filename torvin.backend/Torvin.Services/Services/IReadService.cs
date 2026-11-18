using Torvin.Model.Responses;
using Torvin.Model.SearchObjects;

namespace Torvin.Services.Services;

public interface IReadService<TModel, in TSearch> where TSearch : BaseSearchObject
{
    Task<PagedResult<TModel>> GetPagedAsync(TSearch search, CancellationToken cancellationToken = default);

    Task<TModel> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
