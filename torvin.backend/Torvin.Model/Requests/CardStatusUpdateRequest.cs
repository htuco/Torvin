using System.ComponentModel.DataAnnotations;

namespace Torvin.Model.Requests;

// Nema koda: kod sistemskog statusa koristi state machine i ne smije se promijeniti
public sealed class CardStatusUpdateRequest
{
    [Required(ErrorMessage = "Naziv je obavezan.")]
    [MaxLength(50, ErrorMessage = "Naziv može imati najviše 50 znakova.")]
    public string Name { get; set; } = "";

    [MaxLength(200, ErrorMessage = "Opis može imati najviše 200 znakova.")]
    public string? Description { get; set; }
}
