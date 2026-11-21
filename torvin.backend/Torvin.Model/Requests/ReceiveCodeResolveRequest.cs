using System.ComponentModel.DataAnnotations;

namespace Torvin.Model.Requests;

public sealed class ReceiveCodeResolveRequest
{
    [Required(ErrorMessage = "QR kod je obavezan.")]
    [MaxLength(200, ErrorMessage = "QR kod nije ispravan.")]
    public string Code { get; set; } = "";
}
