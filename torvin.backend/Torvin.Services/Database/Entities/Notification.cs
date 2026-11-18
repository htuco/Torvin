using Torvin.Model.Enums;

namespace Torvin.Services.Database.Entities;

public class Notification : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public NotificationType Type { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    // Zapis na koji se notifikacija odnosi; tip zapisa određuje Type
    public int? RelatedEntityId { get; set; }
}
