using Torvin.Model.Enums;

namespace Torvin.Model.Responses;

// Nazivi polja prate spojena imena (CardType.Name → CardTypeName), pa projekcija ne treba posebno mapiranje.
// Puni broj kartice ne postoji ni u bazi ni ovdje
public sealed record CardResponse(
    int Id,
    int UserId,
    string UserFirstName,
    string UserLastName,
    string? Name,
    int CardTypeId,
    string CardTypeName,
    int CardStatusId,
    string CardStatusCode,
    string CardStatusName,
    int CurrencyId,
    string CurrencyCode,
    CardNetwork Network,
    string Last4Digits,
    DateOnly ExpiresAt,
    decimal Balance,
    bool IsDefault,
    DateTime CreatedAt,
    // Blokirana kartica čiju blokadu je postavio administrator: vlasnik je ne može sam deblokirati.
    // Ne dolazi iz kartice nego iz audit traga, pa ga servis popuni poslije projekcije
    bool IsBlockedByAdmin = false) : IHasId;
