using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Torvin.Model.Constants;
using Torvin.Model.Requests;
using Torvin.Model.Responses;
using Torvin.Model.SearchObjects;
using Torvin.Services.Services;

namespace Torvin.API.Controllers;

// Kartice prijavljenog korisnika. Vlasnik se uzima iz tokena, a tuđa kartica daje 404
[ApiController]
[Route("cards")]
[Authorize(Roles = AppRoles.User)]
public sealed class CardsController(ICardService cardService, IReceiveCodeService receiveCodeService) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<CardResponse>> Get([FromQuery] CardSearchObject search, CancellationToken cancellationToken) =>
        cardService.GetMineAsync(search, cancellationToken);

    [HttpGet("{id:int}")]
    public Task<CardResponse> GetById(int id, CancellationToken cancellationToken) =>
        cardService.GetMineByIdAsync(id, cancellationToken);

    [HttpPost]
    public async Task<IActionResult> Create(CardInsertRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await cardService.CreateAsync(request, cancellationToken));

    [HttpPost("{id:int}/verify")]
    public Task<CardResponse> Verify(int id, CardVerifyRequest request, CancellationToken cancellationToken) =>
        cardService.VerifyAsync(id, request, cancellationToken);

    [HttpPost("{id:int}/resend-code")]
    public async Task<IActionResult> ResendCode(int id, CancellationToken cancellationToken)
    {
        await cardService.ResendCodeAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/block")]
    public Task<CardResponse> Block(int id, CancellationToken cancellationToken) =>
        cardService.BlockAsync(id, cancellationToken);

    [HttpPost("{id:int}/unblock")]
    public Task<CardResponse> Unblock(int id, CancellationToken cancellationToken) =>
        cardService.UnblockAsync(id, cancellationToken);

    [HttpPost("{id:int}/report")]
    public Task<CardResponse> Report(int id, CardReportRequest request, CancellationToken cancellationToken) =>
        cardService.ReportAsync(id, request, cancellationToken);

    // Kod za QR kojim drugi korisnik šalje novac na ovu karticu, opcionalno s predloženim iznosom
    [HttpGet("{id:int}/receive-code")]
    public Task<ReceiveCodeResponse> ReceiveCode(int id, [FromQuery] decimal? amount, CancellationToken cancellationToken) =>
        receiveCodeService.CreateAsync(id, amount, cancellationToken);
}
