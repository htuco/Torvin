using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Torvin.Model.Constants;
using Torvin.Services.Configuration;
using Torvin.Services.Database;
using Torvin.Services.Messaging;

namespace Torvin.Tests;

// Podiže cijeli API u memoriji. Env varijable se postavljaju prije builder-a, jer ih on čita pri kreiranju
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string AllowedOrigin = "http://localhost:3000";
    public const string AdminUsername = "admin.test";
    public const string UserUsername = "korisnik.test";

    private readonly string _databaseName = $"api-{Guid.NewGuid()}";
    private readonly SemaphoreSlim _seedLock = new(1, 1);
    private bool _seeded;

    // Poruke koje bi API poslao na RabbitMQ (npr. mail s kodom)
    public FakeMessagePublisher Publisher { get; } = new();

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

    // Testovi nemaju SQL Server: migracija se preskače, a baza je u memoriji. Kontroleri iz testnog
    // projekta (zaštićeni endpointi za provjeru tokena i role) se dodaju samo ovdje
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services =>
        {
            services.AddScoped<IDatabaseInitializer, NoOpDatabaseInitializer>();
            services.RemoveAll<IMessagePublisher>();
            services.AddSingleton<IMessagePublisher>(Publisher);

            services.RemoveAll<DbContextOptions<TorvinDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<TorvinDbContext>>();
            services.AddDbContext<TorvinDbContext>((provider, options) => options
                .UseInMemoryDatabase(_databaseName)
                .AddInterceptors(
                    provider.GetRequiredService<SoftDeleteInterceptor>(),
                    provider.GetRequiredService<AuditFieldsInterceptor>()));

            services.AddControllers().AddApplicationPart(typeof(ApiFactory).Assembly);
        });

    // Role iz HasData i dva potvrđena korisnika s lozinkom TestUsers.Password
    public async Task SeedUsersAsync()
    {
        await _seedLock.WaitAsync();
        try
        {
            if (_seeded)
            {
                return;
            }

            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TorvinDbContext>();
            await context.Database.EnsureCreatedAsync();
            await TestUsers.AddAsync(context, AdminUsername, AppRoles.Admin);
            await TestUsers.AddAsync(context, UserUsername, AppRoles.User);
            _seeded = true;
        }
        finally
        {
            _seedLock.Release();
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

internal sealed class NoOpDatabaseInitializer : IDatabaseInitializer
{
    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
