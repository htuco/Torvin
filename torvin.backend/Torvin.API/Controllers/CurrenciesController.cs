using Microsoft.AspNetCore.Mvc;
using Torvin.Model.Requests;
using Torvin.Model.Responses;
using Torvin.Model.SearchObjects;
using Torvin.Services.Services;

namespace Torvin.API.Controllers;

[Route("currencies")]
public sealed class CurrenciesController(ICurrencyService service)
    : BaseCRUDController<CurrencyResponse, CurrencySearchObject, CurrencyInsertRequest, CurrencyUpdateRequest>(service);
