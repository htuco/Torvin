namespace Torvin.Services.Database.Entities;

public class AuditLog : BaseEntity
{
    // Prazno kad akciju izvrši sistem (Worker)
    public int? ActorUserId { get; set; }
    public User? ActorUser { get; set; }

    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public int EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? Description { get; set; }
}
