using System.ComponentModel.DataAnnotations;

namespace Torvin.Model.Requests;

public sealed class CardVerifyRequest
{
    [Required(ErrorMessage = "Kod je obavezan.")]
    [RegularExpression("^[0-9]{6}$", ErrorMessage = "Kod ima tačno 6 cifara.")]
    public string Code { get; set; } = "";
}
