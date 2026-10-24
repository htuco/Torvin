using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Torvin.API.Middleware;

namespace Torvin.Tests;

public class CorrelationIdMiddlewareTests
{
    private static async Task<string> InvokeWithAsync(string? incomingId)
    {
        var context = new DefaultHttpContext();
        if (incomingId is not null)
        {
            context.Request.Headers[CorrelationIdMiddleware.HeaderName] = incomingId;
        }

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask,
            NullLogger<CorrelationIdMiddleware>.Instance);
        await middleware.InvokeAsync(context);

        return context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
    }

    [Fact]
    public async Task WithoutIncomingId_GeneratesGuid()
    {
        var id = await InvokeWithAsync(null);

        Assert.True(Guid.TryParse(id, out _));
    }

    [Fact]
    public async Task ValidIncomingId_IsKept()
    {
        var id = await InvokeWithAsync("zahtjev-123");

        Assert.Equal("zahtjev-123", id);
    }

    [Theory]
    [InlineData("lose slovo!\nfalse-log-linija")]
    [InlineData("a-prilicno-dugacak-id-a-prilicno-dugacak-id-a-prilicno-dugacak-id-a-prilicno-dugacak-id")]
    public async Task InvalidIncomingId_IsReplacedWithGuid(string incoming)
    {
        var id = await InvokeWithAsync(incoming);

        Assert.NotEqual(incoming, id);
        Assert.True(Guid.TryParse(id, out _));
    }
}
