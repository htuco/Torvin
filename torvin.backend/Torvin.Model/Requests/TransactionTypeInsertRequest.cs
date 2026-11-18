using System.ComponentModel.DataAnnotations;

namespace Torvin.Model.Requests;

public sealed class TransactionTypeInsertRequest
{
    // Kod je stalan i na njega se veže logika, pa se zadaje samo pri kreiranju
    [Required(ErrorMessage = "Kod je obavezan.")]
    [RegularExpression("^[A-Za-z]{2,30}$", ErrorMessage = "Kod ima 2–30 slova bez razmaka (npr. Expired).")]
    public string Code { get; set; } = "";

    [Required(ErrorMessage = "Naziv je obavezan.")]
    [MaxLength(50, ErrorMessage = "Naziv može imati najviše 50 znakova.")]
    public string Name { get; set; } = "";
}
