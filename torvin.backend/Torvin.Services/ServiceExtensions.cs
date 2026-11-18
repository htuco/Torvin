using Microsoft.Extensions.DependencyInjection;
using Torvin.Services.Security;
using Torvin.Services.Services;

namespace Torvin.Services;

public static class ServiceExtensions
{
    public static IServiceCollection AddTorvinServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddMemoryCache();

        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ITokenRevocationService, TokenRevocationService>();
        services.AddScoped<IEmailCodeService, EmailCodeService>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
