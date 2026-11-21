using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Torvin.Model.Constants;
using Torvin.Model.Requests;
using Torvin.Model.Responses;
using Torvin.Model.SearchObjects;
using Torvin.Services.Services;

namespace Torvin.API.Controllers;

// Pretraga primaoca i razrješavanje skeniranog QR koda za novu transakciju
[ApiController]
[Route("recipients")]
[Authorize(Roles = AppRoles.User)]
public sealed class RecipientsController(IRecipientService recipientService, IReceiveCodeService receiveCodeService)
    : ControllerBase
{
    [HttpGet("search")]
    public Task<PagedResult<RecipientResponse>> Search([FromQuery] RecipientSearchObject search,
        CancellationToken cancellationToken) =>
        recipientService.SearchAsync(search, cancellationToken);

    [HttpPost("resolve-code")]
    public Task<ReceiveCodeRecipientResponse> ResolveCode(ReceiveCodeResolveRequest request,
        CancellationToken cancellationToken) =>
        receiveCodeService.ResolveAsync(request, cancellationToken);
}
