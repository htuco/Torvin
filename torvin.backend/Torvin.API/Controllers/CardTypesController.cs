using Microsoft.AspNetCore.Mvc;
using Torvin.Model.Requests;
using Torvin.Model.Responses;
using Torvin.Model.SearchObjects;
using Torvin.Services.Services;

namespace Torvin.API.Controllers;

[Route("card-types")]
public sealed class CardTypesController(ICardTypeService service)
    : BaseCRUDController<CardTypeResponse, CardTypeSearchObject, CardTypeRequest, CardTypeRequest>(service);
