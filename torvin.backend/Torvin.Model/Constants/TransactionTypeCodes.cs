namespace Torvin.Model.Constants;

public static class TransactionTypeCodes
{
    public const string Transfer = "Transfer";
    public const string Correction = "Correction";
    public const string StandingOrder = "StandingOrder";
    public const string MoneyRequest = "MoneyRequest";

    // Sistemski kodovi: ne brišu se, jer se na njih veže logika
    public static readonly IReadOnlyList<string> All = [Transfer, Correction, StandingOrder, MoneyRequest];
}
