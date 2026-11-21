using Torvin.Model.Enums;

namespace Torvin.Model.Responses;

// Prikaz primaoca iz skeniranog QR koda za ekran nove transakcije. Kartica je samo maskirana, a novac ide po UserId,
// pa server bira primaočevu karticu u valuti pošiljaoca kao kod svakog prenosa
public sealed record ReceiveCodeRecipientResponse(
    int UserId,
    string FirstName,
    string LastName,
    string Username,
    CardNetwork Network,
    string Last4Digits,
    string CurrencyCode,
    decimal? Amount);
