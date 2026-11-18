using Torvin.Model.Enums;

namespace Torvin.Services.Database.Entities;

public class EmailToken : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Postavljen samo kad je svrha aktivacija kartice
    public int? CardId { get; set; }
    public Card? Card { get; set; }

    public EmailTokenPurpose Purpose { get; set; }
    public string CodeHash { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public int AttemptCount { get; set; }
}
