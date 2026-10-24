using System.ComponentModel.DataAnnotations;

namespace Torvin.Services.Configuration;

public sealed class JwtOptions
{
    [Required(ErrorMessage = EnvVars.JwtKey + ConfigRules.NotSet)]
    [MinLength(ConfigRules.JwtKeyMinLength, ErrorMessage = EnvVars.JwtKey + " mora imati najmanje {1} znaka.")]
    public string Key { get; set; } = "";

    [Required(ErrorMessage = EnvVars.JwtIssuer + ConfigRules.NotSet)]
    public string Issuer { get; set; } = "";

    [Required(ErrorMessage = EnvVars.JwtAudience + ConfigRules.NotSet)]
    public string Audience { get; set; } = "";

    [Range(ConfigRules.JwtMinExpiresMinutes, ConfigRules.JwtMaxExpiresMinutes,
        ErrorMessage = EnvVars.JwtExpiresMinutes + " mora biti broj između {1} i {2}.")]
    public int ExpiresMinutes { get; set; }
}
