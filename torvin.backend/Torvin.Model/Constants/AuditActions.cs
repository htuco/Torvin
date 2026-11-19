namespace Torvin.Model.Constants;

// Akcije u audit tragu
public static class AuditActions
{
    public const string CardBlocked = "CardBlocked";

    // Svaki prelaz statusa kartice osim blokiranja, koje ima svoju akciju
    public const string CardStatusChanged = "CardStatusChanged";
    public const string ReversalApproved = "ReversalApproved";
    public const string ReversalRejected = "ReversalRejected";
    public const string UserDeactivated = "UserDeactivated";
}
