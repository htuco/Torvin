using Microsoft.AspNetCore.Mvc.Testing;
using Torvin.Services.Configuration;

namespace Torvin.Tests;

// Podiže cijeli API u memoriji. Env varijable se postavljaju prije builder-a, jer ih on čita pri kreiranju
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string AllowedOrigin = "http://localhost:3000";

    private static readonly Dictionary<string, string> Variables = new()
    {
        [EnvVars.ApiPort] = "5080",
        [EnvVars.ConnectionString] = "Server=test;Database=test",
        [EnvVars.JwtKey] = "0123456789abcdef0123456789abcdef",
        [EnvVars.JwtIssuer] = "torvin-test",
        [EnvVars.JwtAudience] = "torvin-test",
        [EnvVars.JwtExpiresMinutes] = "60",
        [EnvVars.RabbitMqHost] = "rabbitmq-test",
        [EnvVars.RabbitMqPort] = "5672",
        [EnvVars.RabbitMqUser] = "test",
        [EnvVars.RabbitMqPassword] = "test",
        [EnvVars.CorsOrigins] = AllowedOrigin,
    };

    public ApiFactory()
    {
        foreach (var (name, value) in Variables)
        {
            Environment.SetEnvironmentVariable(name, value);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        foreach (var name in Variables.Keys)
        {
            Environment.SetEnvironmentVariable(name, null);
        }
    }
}
