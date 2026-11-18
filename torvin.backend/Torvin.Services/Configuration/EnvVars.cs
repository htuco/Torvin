namespace Torvin.Services.Configuration;

// Imena env varijabli na jednom mjestu: čitanje i poruke grešaka koriste iste konstante
public static class EnvVars
{
    public const string ConnectionString = "CONNECTION_STRING";

    public const string JwtKey = "JWT_KEY";
    public const string JwtIssuer = "JWT_ISSUER";
    public const string JwtAudience = "JWT_AUDIENCE";
    public const string JwtExpiresMinutes = "JWT_EXPIRES_MINUTES";

    public const string RabbitMqHost = "RABBITMQ_HOST";
    public const string RabbitMqPort = "RABBITMQ_PORT";
    public const string RabbitMqUser = "RABBITMQ_USER";
    public const string RabbitMqPassword = "RABBITMQ_PASSWORD";

    public const string SmtpHost = "SMTP_HOST";
    public const string SmtpPort = "SMTP_PORT";
    public const string SmtpUser = "SMTP_USER";
    public const string SmtpPassword = "SMTP_PASSWORD";
    public const string SmtpUseSsl = "SMTP_USE_SSL";
    public const string SmtpFrom = "SMTP_FROM";

    public const string CorsOrigins = "CORS_ORIGINS";
    public const string ApiPort = "API_PORT";

    // Varijable frameworka koje čitamo
    public const string AspNetCoreEnvironment = "ASPNETCORE_ENVIRONMENT";
    public const string DotnetEnvironment = "DOTNET_ENVIRONMENT";
    public const string AspNetCoreUrls = "ASPNETCORE_URLS";
    public const string AspNetCoreHttpPorts = "ASPNETCORE_HTTP_PORTS";
}
