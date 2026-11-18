using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Torvin.Services.Configuration;
using Torvin.Services.Seed;

namespace Torvin.Services.Database;

public static class DatabaseExtensions
{
    // SQL Server kontejner ima samo sa korisnika, a njegova lozinka je SQL_SA_PASSWORD
    private const string DatabaseUser = "sa";

    public static IServiceCollection AddTorvinDatabase(this IServiceCollection services)
    {
        services.AddSingleton<AuditFieldsInterceptor>();
        services.AddSingleton<SoftDeleteInterceptor>();

        services.AddDbContext<TorvinDbContext>((provider, options) =>
        {
            var database = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            // Bez EnableRetryOnFailure: ne dozvoljava transakcije koje otvara kod (promjena balansa).
            // Na bazu pri startu čeka DatabaseInitializer
            options.UseSqlServer(BuildConnectionString(database));
            options.AddInterceptors(
                provider.GetRequiredService<SoftDeleteInterceptor>(),
                provider.GetRequiredService<AuditFieldsInterceptor>());
        });

        services.AddScoped<DataSeeder>();
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
        return services;
    }

    private static string BuildConnectionString(DatabaseOptions database) =>
        new SqlConnectionStringBuilder
        {
            DataSource = $"{database.Host},{database.Port}",
            InitialCatalog = database.Name,
            UserID = DatabaseUser,
            Password = database.Password,
            // Lokalni SQL Server kontejner ima samopotpisan certifikat
            TrustServerCertificate = true,
        }.ConnectionString;
}
