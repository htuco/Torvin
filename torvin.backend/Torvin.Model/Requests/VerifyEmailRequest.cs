using System.ComponentModel.DataAnnotations;

namespace Torvin.Model.Requests;

public sealed class VerifyEmailRequest
{
    [Required(ErrorMessage = "Email je obavezan.")]
    [MaxLength(256, ErrorMessage = "Email može imati najviše 256 znakova.")]
    [EmailAddress(ErrorMessage = "Email nije u ispravnom formatu (npr. ime@domena.ba).")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Kod je obavezan.")]
    [RegularExpression("^[0-9]{6}$", ErrorMessage = "Kod ima tačno 6 cifara.")]
    public string Code { get; set; } = "";
}
