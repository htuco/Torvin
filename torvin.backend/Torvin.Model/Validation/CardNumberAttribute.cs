using System.ComponentModel.DataAnnotations;

namespace Torvin.Model.Validation;

// Broj kartice: tačno 16 cifara, ispravna kontrolna cifra i podržana mreža. Prazna vrijednost je posao atributa [Required]
[AttributeUsage(AttributeTargets.Property)]
public sealed class CardNumberAttribute : ValidationAttribute
{
    public const string InvalidNumber = "Broj kartice nije ispravan. Unesite 16 cifara bez razmaka.";
    public const string UnsupportedNetwork = "Podržane su samo Visa i Mastercard kartice.";

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string number || number.Length == 0)
        {
            return ValidationResult.Success;
        }

        if (number.Length != CardNumber.Length || !CardNumber.IsValidLuhn(number))
        {
            return new ValidationResult(InvalidNumber, MemberNames(validationContext));
        }

        return CardNumber.DetectNetwork(number) is null
            ? new ValidationResult(UnsupportedNetwork, MemberNames(validationContext))
            : ValidationResult.Success;
    }

    private static string[]? MemberNames(ValidationContext context) =>
        context.MemberName is null ? null : [context.MemberName];
}
