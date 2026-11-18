namespace Torvin.Services.Database.Entities;

public class FavoriteRecipient : BaseEntity, ISoftDeletable
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int RecipientUserId { get; set; }
    public User RecipientUser { get; set; } = null!;

    public string? Nickname { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
