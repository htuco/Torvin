using System.ComponentModel.DataAnnotations;

namespace Torvin.Model.Requests;

// Mijenja se samo nadimak. Drugi primalac je novi omiljeni, ne izmjena postojećeg
public sealed class FavoriteRecipientUpdateRequest
{
    [MaxLength(FavoriteRecipientInsertRequest.NicknameMaxLength, ErrorMessage = FavoriteRecipientInsertRequest.NicknameTooLong)]
    public string? Nickname { get; set; }
}
