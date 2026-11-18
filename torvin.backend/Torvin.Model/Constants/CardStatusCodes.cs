namespace Torvin.Model.Constants;

// Kod je nepromjenjiv i na njega se veže logika. Admin mijenja samo naziv i opis statusa
public static class CardStatusCodes
{
    public const string Pending = "Pending";
    public const string Active = "Active";
    public const string Blocked = "Blocked";
    public const string Lost = "Lost";
    public const string Stolen = "Stolen";
    public const string Closed = "Closed";
}
