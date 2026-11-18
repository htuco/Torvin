namespace Torvin.Services.Database.Entities;

public class News : BaseEntity, ISoftDeletable
{
    public int AuthorUserId { get; set; }
    public User AuthorUser { get; set; } = null!;

    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public byte[] Image { get; set; } = [];
    public string ImageContentType { get; set; } = "";
    public DateTime PublishedAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
