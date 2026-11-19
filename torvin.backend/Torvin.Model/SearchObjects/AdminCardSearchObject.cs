namespace Torvin.Model.SearchObjects;

// Admin pretraga traži i po imenu, prezimenu i korisničkom imenu vlasnika
public sealed class AdminCardSearchObject : CardSearchObject
{
    public int? UserId { get; set; }
}
