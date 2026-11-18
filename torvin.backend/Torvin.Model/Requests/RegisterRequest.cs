using System.ComponentModel.DataAnnotations;
using Torvin.Model.Validation;

namespace Torvin.Model.Requests;

// Nema role ni statusa: svaki registrovan korisnik dobija rolu User i nepotvrđen email (upute §5)
public sealed class RegisterRequest
{
    [Required(ErrorMessage = "Ime je obavezno.")]
    [MaxLength(50, ErrorMessage = "Ime može imati najviše 50 znakova.")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Prezime je obavezno.")]
    [MaxLength(50, ErrorMessage = "Prezime može imati najviše 50 znakova.")]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Korisničko ime je obavezno.")]
    [RegularExpression("^[a-zA-Z0-9._]{3,50}$",
        ErrorMessage = "Korisničko ime ima 3–50 znakova: slova, cifre, tačka i donja crta.")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Email je obavezan.")]
    [MaxLength(256, ErrorMessage = "Email može imati najviše 256 znakova.")]
    [EmailAddress(ErrorMessage = "Email nije u ispravnom formatu (npr. ime@domena.ba).")]
    public string Email { get; set; } = "";

    // 6–15 cifara (najduži međunarodni broj ima 15), s po jednim razmakom između, pa stane u 30 znakova
    [RegularExpression(@"^\+?(?:[0-9] ?){5,14}[0-9]$",
        ErrorMessage = "Telefon ima 6–15 cifara, s razmacima između i + na početku po želji (npr. +387 61 234 567).")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Lozinka je obavezna.")]
    [StrongPassword]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Potvrda lozinke je obavezna.")]
    [Compare(nameof(Password), ErrorMessage = "Lozinke se ne poklapaju.")]
    public string ConfirmPassword { get; set; } = "";
}
