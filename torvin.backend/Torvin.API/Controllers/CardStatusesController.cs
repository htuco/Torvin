using Microsoft.AspNetCore.Mvc;
using Torvin.Model.Requests;
using Torvin.Model.Responses;
using Torvin.Model.SearchObjects;
using Torvin.Services.Services;

namespace Torvin.API.Controllers;

[Route("card-statuses")]
public sealed class CardStatusesController(ICardStatusService service)
    : BaseCRUDController<CardStatusResponse, CardStatusSearchObject, CardStatusInsertRequest, CardStatusUpdateRequest>(service);
