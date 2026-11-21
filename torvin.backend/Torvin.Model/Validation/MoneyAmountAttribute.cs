using System.ComponentModel.DataAnnotations;

namespace Torvin.Model.Validation;

// Iznos u granicama i s najviše dvije decimale (Money). Nepostavljen iznos je 0, pa i on dobija istu poruku
[AttributeUsage(AttributeTargets.Property)]
public sealed class MoneyAmountAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not decimal amount || Money.IsValidAmount(amount))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(Money.InvalidAmount,
            validationContext.MemberName is null ? null : [validationContext.MemberName]);
    }
}
