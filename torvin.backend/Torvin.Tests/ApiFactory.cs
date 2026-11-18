using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Torvin.Services.Configuration;
using Torvin.Services.Database;

namespace Torvin.Tests;

// Podiže cijeli API u memoriji. Env varijable se postavljaju prije builder-a, jer ih on čita pri kreiranju
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string AllowedOrigin = "http://localhost:3000";

    private static readonly Dictionary<string, string> Variables = new()
    {
        [EnvVars.ApiPort] = "5080",
        [EnvVars.DbHost] = "sqlserver-test",
        [EnvVars.DbPort] = "1433",
        [EnvVars.DbName] = "test",
        [EnvVars.SqlSaPassword] = "test",
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

    // Testovi nemaju SQL Server, pa se migracija pri startu preskače
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services =>
            services.AddScoped<IDatabaseInitializer, NoOpDatabaseInitializer>());

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        foreach (var name in Variables.Keys)
        {
            Environment.SetEnvironmentVariable(name, null);
        }
    }
}

internal sealed class NoOpDatabaseInitializer : IDatabaseInitializer
{
    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
