namespace Torvin.Services.Database.Entities;

public class Card : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int CardTypeId { get; set; }
    public CardType CardType { get; set; } = null!;

    public int CardStatusId { get; set; }
    public CardStatus CardStatus { get; set; } = null!;

    public int CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;

    public string? Name { get; set; }

    // Puni broj se nikad ne čuva
    public string Last4Digits { get; set; } = "";
    public DateOnly ExpiresAt { get; set; }
    public decimal Balance { get; set; }

    // Kartica na koju stiže novac kad se korisniku plaća po korisniku, a ne po kartici
    public bool IsDefault { get; set; }

    // Dva istovremena upisa balansa: drugi dobija grešku konkurentnosti umjesto da pregazi prvi
    public byte[] RowVersion { get; set; } = [];
}
