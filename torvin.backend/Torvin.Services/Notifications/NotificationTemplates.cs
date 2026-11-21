using System.Globalization;
using Torvin.Model.Constants;
using Torvin.Model.Enums;

namespace Torvin.Services.Notifications;

// Svi naslovi i tekstovi notifikacija na jednom mjestu. Imena se ne mijenjaju po padežima (to se ne može izvesti iz
// imena), pa druga strana ide na kraj teksta: "Pošiljalac: Emir Kovačević."
public static class NotificationTemplates
{
    // Format iznosa ne zavisi od kulture sistema, pa je isti u Dockeru i u testovima: 1.245,00 BAM
    private static readonly NumberFormatInfo AmountFormat = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = ".",
        NumberDecimalDigits = 2,
    };

    public static string Amount(decimal amount, string currencyCode) =>
        $"{amount.ToString("N2", AmountFormat)} {currencyCode}";

    public static NotificationDraft TransactionReceived(int receiverUserId, int transactionId, string amount,
        string receiverCardLast4, string senderName) =>
        new(receiverUserId, NotificationType.TransactionReceived, "Primljena uplata",
            $"Primili ste {amount} na karticu •••• {receiverCardLast4}. Pošiljalac: {senderName}.", transactionId);

    public static NotificationDraft TransactionSent(int senderUserId, int transactionId, string amount,
        string senderCardLast4, string receiverName) =>
        new(senderUserId, NotificationType.TransactionSent, "Uplata poslana",
            $"Poslali ste {amount} s kartice •••• {senderCardLast4}. Primalac: {receiverName}.", transactionId);

    // Razlog se dodaje samo kad je status mijenjao administrator, jer vlasnik zna zašto je sam mijenjao
    public static NotificationDraft CardStatusChanged(int ownerUserId, int cardId, string last4, string newStatusCode,
        string newStatusName, bool changedByAdmin, string? reason)
    {
        var (type, title, message) = newStatusCode switch
        {
            CardStatusCodes.Blocked => (NotificationType.CardBlocked, "Kartica blokirana",
                $"Kartica •••• {last4} je blokirana."),
            CardStatusCodes.Lost => (NotificationType.CardStatusChanged, "Kartica prijavljena kao izgubljena",
                $"Kartica •••• {last4} je prijavljena kao izgubljena i više se ne može koristiti."),
            CardStatusCodes.Stolen => (NotificationType.CardStatusChanged, "Kartica prijavljena kao ukradena",
                $"Kartica •••• {last4} je prijavljena kao ukradena i više se ne može koristiti."),
            _ => (NotificationType.CardStatusChanged, "Status kartice promijenjen",
                $"Administrator je promijenio status kartice •••• {last4} u \"{newStatusName}\"."),
        };

        if (changedByAdmin && !string.IsNullOrWhiteSpace(reason))
        {
            message += $" Razlog: {reason.Trim()}";
        }

        return new NotificationDraft(ownerUserId, type, title, message, cardId);
    }
}
