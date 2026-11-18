using Microsoft.OpenApi;

namespace Torvin.API.Infrastructure;

public static class SwaggerExtensions
{
    private const string SchemeName = "Bearer";

    // Dugme "Authorize" u Swaggeru: token iz /auth/login se zalijepi jednom i ide uz svaki zahtjev
    public static IServiceCollection AddTorvinSwagger(this IServiceCollection services) =>
        services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition(SchemeName, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Token iz odgovora na POST /auth/login.",
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(SchemeName, document)] = [],
            });
        });
}
