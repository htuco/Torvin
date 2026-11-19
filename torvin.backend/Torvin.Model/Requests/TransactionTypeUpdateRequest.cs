using System.ComponentModel.DataAnnotations;

namespace Torvin.Model.Requests;

// Nema koda: kod sistemskog tipa se koristi u logici i ne smije se promijeniti
public sealed class TransactionTypeUpdateRequest
{
    [Required(ErrorMessage = "Naziv je obavezan.")]
    [MaxLength(50, ErrorMessage = "Naziv može imati najviše 50 znakova.")]
    public string Name { get; set; } = "";
}
