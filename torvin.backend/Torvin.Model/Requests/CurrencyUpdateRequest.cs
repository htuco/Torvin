using System.ComponentModel.DataAnnotations;

namespace Torvin.Model.Requests;

// Nema koda: kod valute je stalan
public sealed class CurrencyUpdateRequest
{
    [Required(ErrorMessage = "Naziv je obavezan.")]
    [MaxLength(50, ErrorMessage = "Naziv može imati najviše 50 znakova.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Simbol je obavezan.")]
    [MaxLength(5, ErrorMessage = "Simbol može imati najviše 5 znakova.")]
    public string Symbol { get; set; } = "";
}
