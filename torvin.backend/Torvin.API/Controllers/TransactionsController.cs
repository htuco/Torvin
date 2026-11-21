using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Torvin.Model.Constants;
using Torvin.Model.Requests;
using Torvin.Model.Responses;
using Torvin.Model.SearchObjects;
using Torvin.Services.Services;

namespace Torvin.API.Controllers;

// Interni prenos i historija prijavljenog korisnika. Tuđa transakcija daje 404
[ApiController]
[Route("transactions")]
[Authorize(Roles = AppRoles.User)]
public sealed class TransactionsController(ITransactionService transactionService) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<TransactionResponse>> Get([FromQuery] TransactionSearchObject search,
        CancellationToken cancellationToken) =>
        transactionService.GetMineAsync(search, cancellationToken);

    [HttpGet("{id:int}")]
    public Task<TransactionResponse> GetById(int id, CancellationToken cancellationToken) =>
        transactionService.GetMineByIdAsync(id, cancellationToken);

    [HttpPost]
    public async Task<IActionResult> Create(TransactionInsertRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await transactionService.CreateAsync(request, cancellationToken));
}
