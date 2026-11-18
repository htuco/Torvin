using System.ComponentModel.DataAnnotations;

namespace Torvin.Model.Validation;

// Politika lozinke za svaku novu lozinku (registracija, reset). Prazna vrijednost je posao atributa [Required]
[AttributeUsage(AttributeTargets.Property)]
public sealed class StrongPasswordAttribute : ValidationAttribute
{
    public const int MinLength = 8;
    public const int MaxLength = 100;

    public StrongPasswordAttribute()
        : base("Lozinka mora imati 8–100 znakova, veliko i malo slovo, broj i specijalni znak.")
    {
    }

    public override bool IsValid(object? value) =>
        value is not string password
        || (password.Length is >= MinLength and <= MaxLength
            && password.Any(char.IsUpper)
            && password.Any(char.IsLower)
            && password.Any(char.IsDigit)
            && password.Any(c => !char.IsLetterOrDigit(c)));
}
