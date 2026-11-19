using Torvin.Model.Enums;

namespace Torvin.Services.Database.Entities;

public class ChatMessage : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public ChatMessageRole Role { get; set; }
    public string Content { get; set; } = "";
}
