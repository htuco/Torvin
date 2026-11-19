using System.ComponentModel.DataAnnotations;

namespace Torvin.Services.Configuration;

public sealed class ApiOptions
{
    [Range(ConfigRules.MinPort, ConfigRules.MaxPort,
        ErrorMessage = EnvVars.ApiPort + " mora biti broj između {1} i {2}.")]
    public int Port { get; set; }
}
