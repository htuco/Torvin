namespace Torvin.Services.Database.Entities;

public class SavingsGoal : BaseEntity, ISoftDeletable
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Name { get; set; } = "";
    public decimal TargetAmount { get; set; }

    // Zbir uplata i povlačenja, mijenja se u istoj transakciji kao i pokret
    public decimal CurrentAmount { get; set; }

    public int CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;

    public DateOnly? Deadline { get; set; }
    public DateTime? AchievedAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<SavingsMovement> Movements { get; set; } = [];
}
