using Torvin.Model.Enums;

namespace Torvin.Model.Responses;

// Transakcija iz pogleda prijavljenog korisnika: smjer, njegova kartica i druga strana. Od tuđe kartice se vide
// samo zadnje 4 cifre, a ID druge strane služi za dodavanje u omiljene primaoce
public sealed record TransactionResponse(
    int Id,
    string ReferenceNumber,
    TransactionDirection Direction,
    decimal Amount,
    string CurrencyCode,
    string TransactionTypeCode,
    string TransactionTypeName,
    string TransactionStatusCode,
    string TransactionStatusName,
    int? TransactionCategoryId,
    string? TransactionCategoryName,
    string? Note,
    int? MyCardId,
    string? MyCardLast4Digits,
    int CounterpartyUserId,
    string CounterpartyFirstName,
    string CounterpartyLastName,
    string? CounterpartyCardLast4Digits,
    DateTime CreatedAt) : IHasId;
