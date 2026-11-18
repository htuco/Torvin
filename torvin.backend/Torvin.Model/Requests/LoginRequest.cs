using System.ComponentModel.DataAnnotations;
using Torvin.Model.Enums;

namespace Torvin.Model.Requests;

public sealed class LoginRequest
{
    [Required(ErrorMessage = "Korisničko ime ili email je obavezno.")]
    [MaxLength(256, ErrorMessage = "Korisničko ime ili email može imati najviše 256 znakova.")]
    public string Login { get; set; } = "";

    [Required(ErrorMessage = "Lozinka je obavezna.")]
    [MaxLength(100, ErrorMessage = "Lozinka može imati najviše 100 znakova.")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Aplikacija je obavezna (Desktop ili Mobile).")]
    [EnumDataType(typeof(ClientApp), ErrorMessage = "Aplikacija mora biti Desktop ili Mobile.")]
    public ClientApp? App { get; set; }
}
