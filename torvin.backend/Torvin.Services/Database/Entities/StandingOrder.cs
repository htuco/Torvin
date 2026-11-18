using Torvin.Model.Enums;

namespace Torvin.Services.Database.Entities;

public class StandingOrder : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int SourceCardId { get; set; }
    public Card SourceCard { get; set; } = null!;

    // Novac stiže na podrazumijevanu karticu primaoca u trenutku izvršenja
    public int RecipientUserId { get; set; }
    public User RecipientUser { get; set; } = null!;

    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public StandingOrderFrequency Frequency { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateTime? NextExecutionAt { get; set; }
    public StandingOrderStatus Status { get; set; } = StandingOrderStatus.Active;

    public ICollection<StandingOrderExecution> Executions { get; set; } = [];
}
