using Microsoft.AspNetCore.Mvc;
using Torvin.Model.Requests;
using Torvin.Model.Responses;
using Torvin.Model.SearchObjects;
using Torvin.Services.Services;

namespace Torvin.API.Controllers;

[Route("transaction-statuses")]
public sealed class TransactionStatusesController(ITransactionStatusService service)
    : BaseCRUDController<TransactionStatusResponse, TransactionStatusSearchObject, TransactionStatusInsertRequest, TransactionStatusUpdateRequest>(service);
