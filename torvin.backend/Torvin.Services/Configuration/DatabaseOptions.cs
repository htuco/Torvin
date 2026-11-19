using System.ComponentModel.DataAnnotations;

namespace Torvin.Services.Configuration;

// Lozinka je ista varijabla koju koristi i SQL Server kontejner, pa je zapisana samo jednom
public sealed class DatabaseOptions
{
    [Required(ErrorMessage = EnvVars.DbHost + ConfigRules.NotSet)]
    public string Host { get; set; } = "";

    [Range(ConfigRules.MinPort, ConfigRules.MaxPort,
        ErrorMessage = EnvVars.DbPort + " mora biti broj između {1} i {2}.")]
    public int Port { get; set; }

    [Required(ErrorMessage = EnvVars.DbName + ConfigRules.NotSet)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = EnvVars.SqlSaPassword + ConfigRules.NotSet)]
    public string Password { get; set; } = "";
}
