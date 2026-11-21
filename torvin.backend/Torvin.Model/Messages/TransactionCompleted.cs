namespace Torvin.Model.Messages;

// Knjižena transakcija. Objavljuje se na fanout exchange, pa je dobija svaki vezan red (notifikacije, budžet, anomalije)
public sealed record TransactionCompleted(
    Guid MessageId,
    int TransactionId,
    int FromUserId,
    int ToUserId,
    decimal Amount,
    string CurrencyCode,
    int? TransactionCategoryId,
    DateTime OccurredAtUtc) : IMessage;
