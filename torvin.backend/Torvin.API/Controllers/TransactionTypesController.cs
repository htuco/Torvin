using Microsoft.AspNetCore.Mvc;
using Torvin.Model.Requests;
using Torvin.Model.Responses;
using Torvin.Model.SearchObjects;
using Torvin.Services.Services;

namespace Torvin.API.Controllers;

[Route("transaction-types")]
public sealed class TransactionTypesController(ITransactionTypeService service)
    : BaseCRUDController<TransactionTypeResponse, TransactionTypeSearchObject, TransactionTypeInsertRequest, TransactionTypeUpdateRequest>(service);
