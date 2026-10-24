using System.ComponentModel.DataAnnotations;

namespace Torvin.Services.Configuration;

public sealed class SmtpOptions
{
    [Required(ErrorMessage = EnvVars.SmtpHost + ConfigRules.NotSet)]
    public string Host { get; set; } = "";

    [Range(ConfigRules.MinPort, ConfigRules.MaxPort,
        ErrorMessage = EnvVars.SmtpPort + " mora biti broj između {1} i {2}.")]
    public int Port { get; set; }

    // Mailpit u devu ne traži prijavu, pa korisnik i lozinka smiju biti prazni
    public string User { get; set; } = "";

    public string Password { get; set; } = "";

    public bool UseSsl { get; set; }

    [Required(ErrorMessage = EnvVars.SmtpFrom + ConfigRules.NotSet)]
    public string From { get; set; } = "";
}
