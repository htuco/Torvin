using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Torvin.Model.Constants;
using Torvin.Model.Responses;
using Torvin.Model.SearchObjects;
using Torvin.Services.Services;

namespace Torvin.API.Controllers;

// Izmjena referentnih podataka je samo za admina (desktop aplikacija)
public abstract class BaseCRUDController<TModel, TSearch, TInsert, TUpdate>(
    ICRUDService<TModel, TSearch, TInsert, TUpdate> service)
    : BaseReadController<TModel, TSearch>(service)
    where TModel : IHasId
    where TSearch : BaseSearchObject
{
    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<TModel>> Insert(TInsert request, CancellationToken cancellationToken)
    {
        var created = await service.InsertAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public Task<TModel> Update(int id, TUpdate request, CancellationToken cancellationToken) =>
        service.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
