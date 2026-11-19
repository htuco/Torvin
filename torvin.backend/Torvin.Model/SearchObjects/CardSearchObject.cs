namespace Torvin.Model.SearchObjects;

// Fts traži po nazivu kartice i zadnje 4 cifre
public class CardSearchObject : BaseSearchObject
{
    public int? CardStatusId { get; set; }

    public int? CardTypeId { get; set; }
}
