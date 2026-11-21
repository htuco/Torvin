using System.ComponentModel.DataAnnotations;

namespace Torvin.Model.Requests;

// Primalac je korisnik iz pretrage ili iz QR koda (resolve-code vraća njegov UserId), a ne ID tuđe kartice
public sealed class FavoriteRecipientInsertRequest
{
    public const int NicknameMaxLength = 50;
    public const string NicknameTooLong = "Nadimak može imati najviše 50 znakova.";

    [Range(1, int.MaxValue, ErrorMessage = "Primalac je obavezan.")]
    public int RecipientUserId { get; set; }

    [MaxLength(NicknameMaxLength, ErrorMessage = NicknameTooLong)]
    public string? Nickname { get; set; }
}
