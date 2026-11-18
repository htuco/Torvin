namespace Torvin.Services.Database.Entities;

public class Currency : BaseEntity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Symbol { get; set; } = "";
}
