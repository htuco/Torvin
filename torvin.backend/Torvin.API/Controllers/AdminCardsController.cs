using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Torvin.Model.Constants;
using Torvin.Model.Requests;
using Torvin.Model.Responses;
using Torvin.Model.SearchObjects;
using Torvin.Services.Services;

namespace Torvin.API.Controllers;

// Pregled svih kartica i ručna promjena statusa uz obavezan razlog (prijava §4.1)
[ApiController]
[Route("admin/cards")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class AdminCardsController(ICardService cardService) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<CardResponse>> Get([FromQuery] AdminCardSearchObject search, CancellationToken cancellationToken) =>
        cardService.GetAllAsync(search, cancellationToken);

    [HttpPut("{id:int}/status")]
    public Task<CardResponse> ChangeStatus(int id, CardStatusChangeRequest request, CancellationToken cancellationToken) =>
        cardService.ChangeStatusAsync(id, request, cancellationToken);
}
