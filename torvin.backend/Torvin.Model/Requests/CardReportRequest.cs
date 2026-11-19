using System.ComponentModel.DataAnnotations;
using Torvin.Model.Enums;

namespace Torvin.Model.Requests;

public sealed class CardReportRequest
{
    [Required(ErrorMessage = "Razlog prijave je obavezan (Lost ili Stolen).")]
    [EnumDataType(typeof(CardReportReason), ErrorMessage = "Razlog prijave mora biti Lost ili Stolen.")]
    public CardReportReason? Reason { get; set; }

    [MaxLength(200, ErrorMessage = "Napomena može imati najviše 200 znakova.")]
    public string? Note { get; set; }
}
