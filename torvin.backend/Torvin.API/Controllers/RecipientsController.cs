using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Torvin.Model.Constants;
using Torvin.Model.Responses;
using Torvin.Model.SearchObjects;
using Torvin.Services.Services;

namespace Torvin.API.Controllers;

// Pretraga primaoca za novu transakciju
[ApiController]
[Route("recipients")]
[Authorize(Roles = AppRoles.User)]
public sealed class RecipientsController(IRecipientService recipientService) : ControllerBase
{
    [HttpGet("search")]
    public Task<PagedResult<RecipientResponse>> Search([FromQuery] RecipientSearchObject search,
        CancellationToken cancellationToken) =>
        recipientService.SearchAsync(search, cancellationToken);
}
