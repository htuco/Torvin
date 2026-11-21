using System.ComponentModel.DataAnnotations;
using Torvin.Model.Validation;

namespace Torvin.Model.Requests;

// Valuta se ne šalje: transakcija je uvijek u valuti kartice pošiljaoca. Primalac je korisnik, a njegovu karticu
// bira server, pa klijent nikad ne šalje ID tuđe kartice
public sealed class TransactionInsertRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Kartica je obavezna.")]
    public int FromCardId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Primalac je obavezan.")]
    public int ToUserId { get; set; }

    [MoneyAmount]
    public decimal Amount { get; set; }

    [MaxLength(200, ErrorMessage = "Napomena može imati najviše 200 znakova.")]
    public string? Note { get; set; }

    public int? TransactionCategoryId { get; set; }
}
