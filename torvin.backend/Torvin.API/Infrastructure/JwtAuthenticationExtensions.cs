using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Torvin.Model.Exceptions;
using Torvin.Model.Responses;
using Torvin.Services.Configuration;
using Torvin.Services.Security;

namespace Torvin.API.Infrastructure;

public static class JwtAuthenticationExtensions
{
    public const string UnauthorizedMessage = UnauthorizedException.DefaultMessage;
    public const string ForbiddenMessage = "Nemate pravo pristupa ovoj akciji.";

    public static IServiceCollection AddTorvinJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Ključ, issuer i audience dolaze iz .env preko JwtOptions, a ne iz appsettings.json
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = JwtTokenService.SigningKey(jwt.Key),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = TorvinClaims.UserId,
                    RoleClaimType = TorvinClaims.Role,
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = RejectRevokedTokenAsync,
                    OnChallenge = WriteUnauthorizedAsync,
                    OnForbidden = WriteForbiddenAsync,
                };
            });

        services.AddAuthorization();
        return services;
    }

    // Potpis i rok su ispravni, ali token je možda odjavljen
    private static async Task RejectRevokedTokenAsync(TokenValidatedContext context)
    {
        var tokenId = context.Principal?.FindFirst(TorvinClaims.TokenId)?.Value;
        var revocation = context.HttpContext.RequestServices.GetRequiredService<ITokenRevocationService>();
        if (tokenId is null || await revocation.IsRevokedAsync(tokenId, context.HttpContext.RequestAborted))
        {
            context.Fail("Token je opozvan.");
        }
    }

    // 401 i 403 imaju isto tijelo kao i ostale greške API-ja
    private static async Task WriteUnauthorizedAsync(JwtBearerChallengeContext context)
    {
        context.HandleResponse();
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.WWWAuthenticate = JwtBearerDefaults.AuthenticationScheme;
        await context.Response.WriteAsJsonAsync(new ErrorResponse(UnauthorizedMessage));
    }

    private static async Task WriteForbiddenAsync(ForbiddenContext context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new ErrorResponse(ForbiddenMessage));
    }
}
