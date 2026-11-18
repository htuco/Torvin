using Torvin.Model.Exceptions;
using Torvin.Model.Responses;

namespace Torvin.API.Middleware;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    private const string GenericMessage = "Došlo je do greške. Pokušajte ponovo.";

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Klijent je prekinuo zahtjev, nema greške za prijavu
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            var (statusCode, body) = Map(exception);

            if (statusCode == StatusCodes.Status500InternalServerError)
            {
                logger.LogError(exception, "Neobrađena greška: {Method} {Path}",
                    context.Request.Method, context.Request.Path);
            }
            else
            {
                logger.LogWarning("Zahtjev odbijen ({StatusCode}): {Message}", statusCode, exception.Message);
            }

            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(body);
        }
    }

    private static (int StatusCode, ErrorResponse Body) Map(Exception exception) => exception switch
    {
        ValidationException validation => (StatusCodes.Status400BadRequest,
            new ErrorResponse(validation.Message, validation.Errors)),
        BusinessException business => (StatusCodes.Status400BadRequest, new ErrorResponse(business.Message)),
        ForbiddenException forbidden => (StatusCodes.Status403Forbidden, new ErrorResponse(forbidden.Message)),
        NotFoundException notFound => (StatusCodes.Status404NotFound, new ErrorResponse(notFound.Message)),
        _ => (StatusCodes.Status500InternalServerError, new ErrorResponse(GenericMessage)),
    };
}
