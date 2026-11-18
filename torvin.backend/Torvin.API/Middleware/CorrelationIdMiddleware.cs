using System.Text.RegularExpressions;

namespace Torvin.API.Middleware;

public sealed partial class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        // Id od klijenta se prihvata samo ako je kratak i bez posebnih znakova, da ne može zagaditi log
        var incoming = context.Request.Headers[HeaderName].ToString();
        var correlationId = ValidId().IsMatch(incoming) ? incoming : Guid.NewGuid().ToString();

        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }

    [GeneratedRegex("^[A-Za-z0-9-]{1,64}$")]
    private static partial Regex ValidId();
}
