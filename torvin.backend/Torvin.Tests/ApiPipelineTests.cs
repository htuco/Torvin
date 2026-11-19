using System.Net;
using Microsoft.AspNetCore.Hosting;
using Torvin.API.Middleware;

namespace Torvin.Tests;

public class ApiPipelineTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static HttpRequestMessage Preflight(string origin) =>
        new(HttpMethod.Options, "/nepostojeca")
        {
            Headers =
            {
                { "Origin", origin },
                { "Access-Control-Request-Method", "GET" },
            },
        };

    [Fact]
    public async Task UnknownRoute_Returns404WithCorrelationIdHeader()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/nepostojeca");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(Guid.TryParse(response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single(), out _));
    }

    [Fact]
    public async Task ValidCorrelationIdFromRequest_IsEchoedInResponse()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/nepostojeca");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, "zahtjev-123");

        var response = await client.SendAsync(request);

        Assert.Equal("zahtjev-123", response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single());
    }

    [Fact]
    public async Task Cors_AllowsConfiguredOrigin()
    {
        using var client = factory.CreateClient();

        var response = await client.SendAsync(Preflight(ApiFactory.AllowedOrigin));

        Assert.Equal(ApiFactory.AllowedOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Cors_RejectsUnknownOrigin()
    {
        using var client = factory.CreateClient();

        var response = await client.SendAsync(Preflight("http://evil.example"));

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Swagger_IsAvailableInDevelopment()
    {
        using var client = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development")).CreateClient();

        var response = await client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_ReturnsNotFoundInProduction()
    {
        using var client = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production")).CreateClient();

        var response = await client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
