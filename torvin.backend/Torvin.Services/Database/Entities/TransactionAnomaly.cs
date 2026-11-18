namespace Torvin.Services.Database.Entities;

// Skor po signalu se čuva zasebno, da se objasni zašto je transakcija označena
public class TransactionAnomaly : BaseEntity
{
    public int TransactionId { get; set; }
    public Transaction Transaction { get; set; } = null!;

    public decimal Score { get; set; }
    public decimal AmountScore { get; set; }
    public decimal FrequencyScore { get; set; }
    public decimal TimeScore { get; set; }
    public decimal RecipientScore { get; set; }
    public bool IsAnomaly { get; set; }
}
