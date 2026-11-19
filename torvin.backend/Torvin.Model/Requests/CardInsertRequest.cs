using System.ComponentModel.DataAnnotations;
using Torvin.Model.Validation;

namespace Torvin.Model.Requests;

// Klasa, a ne record, da se broj kartice ne pojavi u automatskom ToString (npr. u logu)
public sealed class CardInsertRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Tip kartice je obavezan.")]
    public int CardTypeId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Valuta je obavezna.")]
    public int CurrencyId { get; set; }

    [MaxLength(50, ErrorMessage = "Naziv može imati najviše 50 znakova.")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Broj kartice je obavezan.")]
    [CardNumber]
    public string CardNumber { get; set; } = "";

    [Required(ErrorMessage = "Datum isteka je obavezan.")]
    [RegularExpression("^(0[1-9]|1[0-2])/[0-9]{2}$", ErrorMessage = "Datum isteka je u formatu MM/GG (npr. 08/29).")]
    public string Expiry { get; set; } = "";
}
