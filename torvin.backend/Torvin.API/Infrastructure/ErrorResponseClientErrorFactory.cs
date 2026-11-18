using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Torvin.Model.Responses;

namespace Torvin.API.Infrastructure;

// Greške koje MVC sam vraća (npr. 415 kad tijelo nije JSON) idu istim tijelom kao ostale greške API-ja,
// umjesto ProblemDetails s engleskim naslovom
public sealed class ErrorResponseClientErrorFactory : IClientErrorFactory
{
    public IActionResult GetClientError(ActionContext actionContext, IClientErrorActionResult clientError) =>
        new ObjectResult(new ErrorResponse(Message(clientError.StatusCode))) { StatusCode = clientError.StatusCode };

    private static string Message(int? statusCode) => statusCode switch
    {
        StatusCodes.Status404NotFound => "Traženi podatak ne postoji.",
        StatusCodes.Status415UnsupportedMediaType => "Tijelo zahtjeva mora biti JSON (Content-Type: application/json).",
        _ => "Zahtjev nije ispravan.",
    };
}
