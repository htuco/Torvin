using System.ComponentModel.DataAnnotations;

namespace Torvin.Model.Requests;

public sealed class CurrencyInsertRequest
{
    // Kod je stalan, pa se zadaje samo pri kreiranju
    [Required(ErrorMessage = "Kod valute je obavezan.")]
    [RegularExpression("^[A-Z]{3}$", ErrorMessage = "Kod valute su tri velika slova po ISO 4217 (npr. EUR).")]
    public string Code { get; set; } = "";

    [Required(ErrorMessage = "Naziv je obavezan.")]
    [MaxLength(50, ErrorMessage = "Naziv može imati najviše 50 znakova.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Simbol je obavezan.")]
    [MaxLength(5, ErrorMessage = "Simbol može imati najviše 5 znakova.")]
    public string Symbol { get; set; } = "";
}
