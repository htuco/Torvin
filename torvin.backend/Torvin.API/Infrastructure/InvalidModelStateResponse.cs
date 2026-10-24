using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Torvin.Model.Exceptions;
using Torvin.Model.Responses;

namespace Torvin.API.Infrastructure;

public static class InvalidModelStateResponse
{
    private const string InvalidValueMessage = "Vrijednost nije ispravna.";

    // Automatska validacija modela ide istim tijelom kao i naši izuzeci
    public static IActionResult Create(ActionContext context)
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => ToCamelCase(entry.Key),
                entry => entry.Value!.Errors.Select(ReadMessage).ToArray());

        return new BadRequestObjectResult(new ErrorResponse(ValidationException.DefaultMessage, errors));
    }

    // Greška parsiranja nosi tehničke detalje, pa se klijentu ne prosljeđuje
    private static string ReadMessage(Microsoft.AspNetCore.Mvc.ModelBinding.ModelError error) =>
        error.Exception is null && !string.IsNullOrEmpty(error.ErrorMessage)
            ? error.ErrorMessage
            : InvalidValueMessage;

    private static string ToCamelCase(string key) =>
        string.Join('.', key.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}
