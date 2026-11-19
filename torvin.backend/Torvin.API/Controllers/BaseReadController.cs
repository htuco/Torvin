using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Torvin.Model.Responses;
using Torvin.Model.SearchObjects;
using Torvin.Services.Services;

namespace Torvin.API.Controllers;

// Čitanje je za svakog prijavljenog korisnika, jer liste pune dropdowne u obje aplikacije
[ApiController]
[Authorize]
public abstract class BaseReadController<TModel, TSearch>(IReadService<TModel, TSearch> service) : ControllerBase
    where TSearch : BaseSearchObject
{
    [HttpGet]
    public Task<PagedResult<TModel>> Get([FromQuery] TSearch search, CancellationToken cancellationToken) =>
        service.GetPagedAsync(search, cancellationToken);

    [HttpGet("{id:int}")]
    public Task<TModel> GetById(int id, CancellationToken cancellationToken) =>
        service.GetByIdAsync(id, cancellationToken);
}
