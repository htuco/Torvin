namespace Torvin.Services.Database.Entities;

public class Budget : BaseEntity, ISoftDeletable
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int TransactionCategoryId { get; set; }
    public TransactionCategory TransactionCategory { get; set; } = null!;

    public int CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;

    public int Year { get; set; }
    public int Month { get; set; }
    public decimal LimitAmount { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
