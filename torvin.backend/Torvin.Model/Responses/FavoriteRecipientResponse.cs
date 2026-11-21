namespace Torvin.Model.Responses;

// Email primaoca se ne vraća, kao ni u pretrazi primalaca
public sealed record FavoriteRecipientResponse(
    int Id,
    int RecipientUserId,
    string FirstName,
    string LastName,
    string Username,
    string? Nickname,
    DateTime CreatedAt) : IHasId;
