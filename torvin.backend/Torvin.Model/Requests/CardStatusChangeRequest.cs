using System.ComponentModel.DataAnnotations;

namespace Torvin.Model.Requests;

// Ručna promjena statusa kartice od strane administratora. Razlog ostaje u audit tragu
public sealed class CardStatusChangeRequest
{
    public const int ReasonMinLength = 5;
    public const int ReasonMaxLength = 200;
    public const string ReasonLengthMessage = "Razlog mora imati 5–200 znakova.";

    [Range(1, int.MaxValue, ErrorMessage = "Status je obavezan.")]
    public int CardStatusId { get; set; }

    // Bez podrazumijevane prazne vrijednosti, da izostavljen razlog dobije samo poruku "obavezan", a ne i poruku o dužini
    [Required(ErrorMessage = "Razlog je obavezan.")]
    [StringLength(ReasonMaxLength, MinimumLength = ReasonMinLength, ErrorMessage = ReasonLengthMessage)]
    public string? Reason { get; set; }
}
