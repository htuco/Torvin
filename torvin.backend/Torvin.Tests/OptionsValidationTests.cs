using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Torvin.Services.Configuration;

namespace Torvin.Tests;

public class OptionsValidationTests
{
    private static ServiceProvider BuildProvider(Dictionary<string, string?> variables, Action<IServiceCollection> register)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(variables).Build());
        register(services);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void MissingVariables_ErrorNamesVariables()
    {
        using var provider = BuildProvider([], services => services.AddRabbitMqOptions());

        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<RabbitMqOptions>>().Value);

        Assert.Contains("RABBITMQ_HOST nije zadan.", exception.Message);
        Assert.Contains("RABBITMQ_PORT", exception.Message);
        Assert.Contains("RABBITMQ_USER nije zadan.", exception.Message);
        Assert.Contains("RABBITMQ_PASSWORD nije zadan.", exception.Message);
    }

    [Fact]
    public void ShortJwtKey_ErrorDoesNotContainValue()
    {
        var variables = new Dictionary<string, string?>
        {
            ["JWT_KEY"] = "tajni-kljuc",
            ["JWT_ISSUER"] = "torvin",
            ["JWT_AUDIENCE"] = "torvin",
            ["JWT_EXPIRES_MINUTES"] = "60",
        };
        using var provider = BuildProvider(variables, services => services.AddJwtOptions());

        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<JwtOptions>>().Value);

        Assert.Contains("JWT_KEY mora imati najmanje 32 znaka.", exception.Message);
        Assert.DoesNotContain("tajni-kljuc", exception.Message);
    }

    [Fact]
    public void InvalidPort_ErrorNamesVariableWithoutValue()
    {
        var variables = new Dictionary<string, string?>
        {
            ["SMTP_HOST"] = "mailpit",
            ["SMTP_PORT"] = "nije-broj",
            ["SMTP_FROM"] = "torvin@torvin.local",
        };
        using var provider = BuildProvider(variables, services => services.AddSmtpOptions());

        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<SmtpOptions>>().Value);

        Assert.Contains("SMTP_PORT mora biti broj između 1 i 65535.", exception.Message);
        Assert.DoesNotContain("nije-broj", exception.Message);
    }

    [Fact]
    public void SmtpWithoutCredentials_IsValid()
    {
        var variables = new Dictionary<string, string?>
        {
            ["SMTP_HOST"] = "mailpit",
            ["SMTP_PORT"] = "1025",
            ["SMTP_FROM"] = "torvin@torvin.local",
        };
        using var provider = BuildProvider(variables, services => services.AddSmtpOptions());

        var options = provider.GetRequiredService<IOptions<SmtpOptions>>().Value;

        Assert.Equal(1025, options.Port);
        Assert.Equal("", options.User);
        Assert.False(options.UseSsl);
    }

    [Fact]
    public void InvalidApiPort_ErrorNamesVariableWithoutValue()
    {
        using var provider = BuildProvider(
            new Dictionary<string, string?> { [EnvVars.ApiPort] = "70000" },
            services => services.AddApiOptions());

        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<ApiOptions>>().Value);

        Assert.Contains("API_PORT mora biti broj između 1 i 65535.", exception.Message);
        Assert.DoesNotContain("70000", exception.Message);
    }

    [Fact]
    public void CorsOrigins_AreSplitAndTrimmed()
    {
        var variables = new Dictionary<string, string?> { ["CORS_ORIGINS"] = "http://localhost:5000, http://localhost:3000" };
        using var provider = BuildProvider(variables, services => services.AddCorsOptions());

        var options = provider.GetRequiredService<IOptions<CorsOptions>>().Value;

        Assert.Equal(["http://localhost:5000", "http://localhost:3000"], options.Origins);
    }

    [Fact]
    public void MissingCorsOrigins_ErrorNamesVariable()
    {
        using var provider = BuildProvider([], services => services.AddCorsOptions());

        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<CorsOptions>>().Value);

        Assert.Contains("CORS_ORIGINS nije zadan.", exception.Message);
    }
}
