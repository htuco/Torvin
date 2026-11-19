using Torvin.Model.Enums;

namespace Torvin.Services.Database.Entities;

public class MoneyRequest : BaseEntity
{
    public int RequesterUserId { get; set; }
    public User RequesterUser { get; set; } = null!;

    public int PayerUserId { get; set; }
    public User PayerUser { get; set; } = null!;

    public decimal Amount { get; set; }

    public int CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;

    public string? Note { get; set; }
    public MoneyRequestStatus Status { get; set; } = MoneyRequestStatus.Pending;
    public DateTime? RespondedAt { get; set; }

    public int? TransactionId { get; set; }
    public Transaction? Transaction { get; set; }
}
