using Torvin.Model.Enums;

namespace Torvin.Services.Database.Entities;

public class ReversalRequest : BaseEntity
{
    public int TransactionId { get; set; }
    public Transaction Transaction { get; set; } = null!;

    public int RequestedByUserId { get; set; }
    public User RequestedByUser { get; set; } = null!;

    public string Reason { get; set; } = "";
    public ReversalRequestStatus Status { get; set; } = ReversalRequestStatus.Pending;

    public int? DecidedByUserId { get; set; }
    public User? DecidedByUser { get; set; }
    public string? DecisionReason { get; set; }
    public DateTime? DecidedAt { get; set; }

    public int? CorrectiveTransactionId { get; set; }
    public Transaction? CorrectiveTransaction { get; set; }
}
