using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Torvin.API.Middleware;
using Torvin.Model.Exceptions;

namespace Torvin.Tests;

public class ExceptionMiddlewareTests
{
    private static async Task<(int StatusCode, string Body)> InvokeWithAsync(Exception exception)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionMiddleware(_ => throw exception, NullLogger<ExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        return (context.Response.StatusCode, await new StreamReader(context.Response.Body).ReadToEndAsync());
    }

    [Fact]
    public async Task NotFoundException_Returns404WithMessageAndNoStackTrace()
    {
        var (statusCode, body) = await InvokeWithAsync(new NotFoundException("Kartica nije pronađena."));

        Assert.Equal(StatusCodes.Status404NotFound, statusCode);
        Assert.Contains("Kartica nije pronađena.", body);
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(" at ", body);
    }

    [Fact]
    public async Task BusinessException_Returns400WithMessage()
    {
        var (statusCode, body) = await InvokeWithAsync(new BusinessException("Nedovoljno sredstava."));

        Assert.Equal(StatusCodes.Status400BadRequest, statusCode);
        Assert.Contains("Nedovoljno sredstava.", body);
        Assert.DoesNotContain("errors", body);
    }

    [Fact]
    public async Task ForbiddenException_Returns403()
    {
        var (statusCode, _) = await InvokeWithAsync(new ForbiddenException("Nemate pristup."));

        Assert.Equal(StatusCodes.Status403Forbidden, statusCode);
    }

    [Fact]
    public async Task ValidationException_Returns400WithErrorsPerField()
    {
        var errors = new Dictionary<string, string[]> { ["email"] = ["Email nije u ispravnom formatu."] };

        var (statusCode, body) = await InvokeWithAsync(new ValidationException(errors));

        Assert.Equal(StatusCodes.Status400BadRequest, statusCode);
        using var json = JsonDocument.Parse(body);
        Assert.Equal("Email nije u ispravnom formatu.",
            json.RootElement.GetProperty("errors").GetProperty("email")[0].GetString());
    }

    [Fact]
    public async Task UnknownException_Returns500WithGenericMessage()
    {
        var (statusCode, body) = await InvokeWithAsync(new InvalidOperationException("interna tajna"));

        Assert.Equal(StatusCodes.Status500InternalServerError, statusCode);
        Assert.Contains("Došlo je do greške.", body);
        Assert.DoesNotContain("interna tajna", body);
    }

    [Fact]
    public async Task RequestAborted_WritesNothing()
    {
        using var cancellation = new CancellationTokenSource();
        var context = new DefaultHttpContext { RequestAborted = cancellation.Token };
        context.Response.Body = new MemoryStream();
        await cancellation.CancelAsync();
        var middleware = new ExceptionMiddleware(
            _ => throw new OperationCanceledException(), NullLogger<ExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(0, context.Response.Body.Length);
    }
}
