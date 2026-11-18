namespace Torvin.Services.Database.Entities;

public class Transaction : BaseEntity
{
    // Prikazuje se korisniku umjesto ID-a
    public string ReferenceNumber { get; set; } = "";

    public int SenderUserId { get; set; }
    public User SenderUser { get; set; } = null!;

    public int ReceiverUserId { get; set; }
    public User ReceiverUser { get; set; } = null!;

    public int? SenderCardId { get; set; }
    public Card? SenderCard { get; set; }

    public int? ReceiverCardId { get; set; }
    public Card? ReceiverCard { get; set; }

    public decimal Amount { get; set; }

    public int CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;

    public int TransactionTypeId { get; set; }
    public TransactionType TransactionType { get; set; } = null!;

    public int TransactionStatusId { get; set; }
    public TransactionStatus TransactionStatus { get; set; } = null!;

    public int? TransactionCategoryId { get; set; }
    public TransactionCategory? TransactionCategory { get; set; }

    // Postavljeno na korektivnoj transakciji: koju transakciju poništava
    public int? OriginalTransactionId { get; set; }
    public Transaction? OriginalTransaction { get; set; }

    public string? Note { get; set; }
}
