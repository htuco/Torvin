using System.ComponentModel.DataAnnotations;

namespace Torvin.Services.Configuration;

public sealed class CorsOptions
{
    [MinLength(1, ErrorMessage = EnvVars.CorsOrigins + ConfigRules.NotSet)]
    public string[] Origins { get; set; } = [];
}
