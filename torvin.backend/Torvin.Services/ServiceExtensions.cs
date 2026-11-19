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

        services.AddSingleton<ReferenceDataCache>();
        services.AddScoped<ICardTypeService, CardTypeService>();
        services.AddScoped<ICardStatusService, CardStatusService>();
        services.AddScoped<ICurrencyService, CurrencyService>();
        services.AddScoped<ITransactionTypeService, TransactionTypeService>();
        services.AddScoped<ITransactionStatusService, TransactionStatusService>();
        services.AddScoped<ITransactionCategoryService, TransactionCategoryService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<ICardService, CardService>();
        return services;
    }
}
