using System.ComponentModel.DataAnnotations;

namespace Torvin.Model.Requests;

// Isti request za kreiranje i izmjenu, jer kategorija nema stalni kod
public sealed class TransactionCategoryRequest
{
    [Required(ErrorMessage = "Naziv je obavezan.")]
    [MaxLength(50, ErrorMessage = "Naziv može imati najviše 50 znakova.")]
    public string Name { get; set; } = "";

    [MaxLength(200, ErrorMessage = "Opis može imati najviše 200 znakova.")]
    public string? Description { get; set; }
}
