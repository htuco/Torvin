namespace Torvin.Services.Database.Entities;

public class CardType : BaseEntity
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}
