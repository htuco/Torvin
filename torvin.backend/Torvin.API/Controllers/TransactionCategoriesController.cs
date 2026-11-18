using Microsoft.AspNetCore.Mvc;
using Torvin.Model.Requests;
using Torvin.Model.Responses;
using Torvin.Model.SearchObjects;
using Torvin.Services.Services;

namespace Torvin.API.Controllers;

[Route("transaction-categories")]
public sealed class TransactionCategoriesController(ITransactionCategoryService service)
    : BaseCRUDController<TransactionCategoryResponse, TransactionCategorySearchObject, TransactionCategoryRequest, TransactionCategoryRequest>(service);
