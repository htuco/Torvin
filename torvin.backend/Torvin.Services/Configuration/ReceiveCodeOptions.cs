using System.ComponentModel.DataAnnotations;

namespace Torvin.Services.Configuration;

// Ključ kojim se potpisuje QR kod za primanje. Odvojen je od JWT ključa: jedan ključ, jedna namjena
public sealed class ReceiveCodeOptions
{
    [Required(ErrorMessage = EnvVars.ReceiveCodeKey + ConfigRules.NotSet)]
    [MinLength(ConfigRules.ReceiveCodeKeyMinLength, ErrorMessage = EnvVars.ReceiveCodeKey + " mora imati najmanje {1} znaka.")]
    public string Key { get; set; } = "";
}
