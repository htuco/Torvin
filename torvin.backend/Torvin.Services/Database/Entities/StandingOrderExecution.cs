using Torvin.Model.Enums;

namespace Torvin.Services.Database.Entities;

public class StandingOrderExecution : BaseEntity
{
    public int StandingOrderId { get; set; }
    public StandingOrder StandingOrder { get; set; } = null!;

    public DateTime ScheduledFor { get; set; }
    public int AttemptNumber { get; set; }
    public StandingOrderExecutionStatus Status { get; set; }
    public string? FailureReason { get; set; }

    public int? TransactionId { get; set; }
    public Transaction? Transaction { get; set; }
}
