using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Torvin.Model.Constants;
using Torvin.Model.Requests;
using Torvin.Model.Responses;
using Torvin.Model.SearchObjects;
using Torvin.Services.Services;

namespace Torvin.API.Controllers;

// Omiljeni primaoci prijavljenog korisnika. Vlasnik se uzima iz tokena, a tuđi zapis daje 404
[ApiController]
[Route("favorite-recipients")]
[Authorize(Roles = AppRoles.User)]
public sealed class FavoriteRecipientsController(IFavoriteRecipientService favoriteRecipientService) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<FavoriteRecipientResponse>> Get([FromQuery] FavoriteRecipientSearchObject search,
        CancellationToken cancellationToken) =>
        favoriteRecipientService.GetMineAsync(search, cancellationToken);

    [HttpPost]
    public async Task<IActionResult> Create(FavoriteRecipientInsertRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await favoriteRecipientService.CreateAsync(request, cancellationToken));

    [HttpPut("{id:int}")]
    public Task<FavoriteRecipientResponse> Update(int id, FavoriteRecipientUpdateRequest request,
        CancellationToken cancellationToken) =>
        favoriteRecipientService.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await favoriteRecipientService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
