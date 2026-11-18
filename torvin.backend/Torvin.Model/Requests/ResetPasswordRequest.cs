using System.ComponentModel.DataAnnotations;
using Torvin.Model.Validation;

namespace Torvin.Model.Requests;

public sealed class ResetPasswordRequest
{
    [Required(ErrorMessage = "Email je obavezan.")]
    [MaxLength(256, ErrorMessage = "Email može imati najviše 256 znakova.")]
    [EmailAddress(ErrorMessage = "Email nije u ispravnom formatu (npr. ime@domena.ba).")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Kod je obavezan.")]
    [RegularExpression("^[0-9]{6}$", ErrorMessage = "Kod ima tačno 6 cifara.")]
    public string Code { get; set; } = "";

    [Required(ErrorMessage = "Nova lozinka je obavezna.")]
    [StrongPassword]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Potvrda lozinke je obavezna.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Lozinke se ne poklapaju.")]
    public string ConfirmPassword { get; set; } = "";
}
