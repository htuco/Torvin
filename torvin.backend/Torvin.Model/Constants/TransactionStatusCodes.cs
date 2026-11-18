namespace Torvin.Model.Constants;

public static class TransactionStatusCodes
{
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string Reversed = "Reversed";

    // Sistemski kodovi: ne brišu se, jer se na njih veže logika
    public static readonly IReadOnlyList<string> All = [Completed, Failed, Reversed];
}
