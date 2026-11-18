using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Torvin.Services.Configuration;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddDatabaseOptions(this IServiceCollection services) =>
        services.AddEnvOptions<DatabaseOptions>((o, c) =>
        {
            o.ConnectionString = c[EnvVars.ConnectionString] ?? "";
        });

    public static IServiceCollection AddJwtOptions(this IServiceCollection services) =>
        services.AddEnvOptions<JwtOptions>((o, c) =>
        {
            o.Key = c[EnvVars.JwtKey] ?? "";
            o.Issuer = c[EnvVars.JwtIssuer] ?? "";
            o.Audience = c[EnvVars.JwtAudience] ?? "";
            o.ExpiresMinutes = ReadInt(c, EnvVars.JwtExpiresMinutes);
        });

    public static IServiceCollection AddRabbitMqOptions(this IServiceCollection services) =>
        services.AddEnvOptions<RabbitMqOptions>((o, c) =>
        {
            o.Host = c[EnvVars.RabbitMqHost] ?? "";
            o.Port = ReadInt(c, EnvVars.RabbitMqPort);
            o.User = c[EnvVars.RabbitMqUser] ?? "";
            o.Password = c[EnvVars.RabbitMqPassword] ?? "";
        });

    public static IServiceCollection AddSmtpOptions(this IServiceCollection services) =>
        services.AddEnvOptions<SmtpOptions>((o, c) =>
        {
            o.Host = c[EnvVars.SmtpHost] ?? "";
            o.Port = ReadInt(c, EnvVars.SmtpPort);
            o.User = c[EnvVars.SmtpUser] ?? "";
            o.Password = c[EnvVars.SmtpPassword] ?? "";
            o.UseSsl = bool.TryParse(c[EnvVars.SmtpUseSsl], out var useSsl) && useSsl;
            o.From = c[EnvVars.SmtpFrom] ?? "";
        });

    public static IServiceCollection AddApiOptions(this IServiceCollection services) =>
        services.AddEnvOptions<ApiOptions>((o, c) =>
        {
            o.Port = ReadInt(c, EnvVars.ApiPort);
        });

    public static IServiceCollection AddCorsOptions(this IServiceCollection services) =>
        services.AddEnvOptions<CorsOptions>((o, c) =>
        {
            o.Origins = (c[EnvVars.CorsOrigins] ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        });

    // Varijable su ravna imena iz .env, pa se čitaju ručno, a ne kroz sekcije appsettings.json
    private static IServiceCollection AddEnvOptions<TOptions>(
        this IServiceCollection services, Action<TOptions, IConfiguration> bind)
        where TOptions : class
    {
        services.AddOptions<TOptions>()
            .Configure<IConfiguration>((options, configuration) => bind(options, configuration))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        return services;
    }

    // Neispravan broj postaje 0, pa ga hvata [Range] s porukom koja imenuje varijablu, bez vrijednosti
    private static int ReadInt(IConfiguration configuration, string name) =>
        int.TryParse(configuration[name], out var value) ? value : 0;
}
