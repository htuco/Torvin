namespace Torvin.Services.Database.Entities;

public class CardStatus : BaseEntity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}
