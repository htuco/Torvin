using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Torvin.API.Infrastructure;

// Poruke koje ASP.NET sam piše kad vrijednost ne može vezati za model. Bez ovoga bi korisnik vidio engleski tekst
public static class BosnianModelBindingMessages
{
    public static void Apply(DefaultModelBindingMessageProvider messages)
    {
        messages.SetAttemptedValueIsInvalidAccessor((value, field) => $"Vrijednost '{value}' nije ispravna za polje {field}.");
        messages.SetMissingBindRequiredValueAccessor(field => $"Vrijednost za polje {field} nije poslana.");
        messages.SetMissingKeyOrValueAccessor(() => "Vrijednost je obavezna.");
        messages.SetMissingRequestBodyRequiredValueAccessor(() => "Tijelo zahtjeva je obavezno.");
        messages.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"Vrijednost '{value}' nije ispravna.");
        messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => "Vrijednost nije ispravna.");
        messages.SetNonPropertyValueMustBeANumberAccessor(() => "Vrijednost mora biti broj.");
        messages.SetUnknownValueIsInvalidAccessor(field => $"Vrijednost nije ispravna za polje {field}.");
        messages.SetValueIsInvalidAccessor(value => $"Vrijednost '{value}' nije ispravna.");
        messages.SetValueMustBeANumberAccessor(field => $"Polje {field} mora biti broj.");
        messages.SetValueMustNotBeNullAccessor(value => $"Vrijednost '{value}' ne smije biti prazna.");
    }
}
