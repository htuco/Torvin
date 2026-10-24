using System.ComponentModel.DataAnnotations;

namespace Torvin.Services.Configuration;

public sealed class DatabaseOptions
{
    [Required(ErrorMessage = EnvVars.ConnectionString + ConfigRules.NotSet)]
    public string ConnectionString { get; set; } = "";
}
