using System.ComponentModel.DataAnnotations;

namespace Torvin.Services.Configuration;

public sealed class RabbitMqOptions
{
    [Required(ErrorMessage = EnvVars.RabbitMqHost + ConfigRules.NotSet)]
    public string Host { get; set; } = "";

    [Range(ConfigRules.MinPort, ConfigRules.MaxPort,
        ErrorMessage = EnvVars.RabbitMqPort + " mora biti broj između {1} i {2}.")]
    public int Port { get; set; }

    [Required(ErrorMessage = EnvVars.RabbitMqUser + ConfigRules.NotSet)]
    public string User { get; set; } = "";

    [Required(ErrorMessage = EnvVars.RabbitMqPassword + ConfigRules.NotSet)]
    public string Password { get; set; } = "";
}
