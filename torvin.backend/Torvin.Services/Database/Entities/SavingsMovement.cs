using Torvin.Model.Enums;

namespace Torvin.Services.Database.Entities;

public class SavingsMovement : BaseEntity
{
    public int SavingsGoalId { get; set; }
    public SavingsGoal SavingsGoal { get; set; } = null!;

    public int CardId { get; set; }
    public Card Card { get; set; } = null!;

    public SavingsMovementType Type { get; set; }
    public decimal Amount { get; set; }
}
