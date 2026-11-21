using Microsoft.EntityFrameworkCore;
using Torvin.Model.Messages;
using Torvin.Services.Database;
using Torvin.Services.Notifications;

namespace Torvin.Worker.Consumers;

// Knjižena transakcija: primalac dobija "Primljena uplata", a pošiljalac potvrdu "Uplata poslana".
// Imena i kartice se čitaju iz baze, a iznos i valuta iz poruke
public sealed class TransactionCompletedHandler(
    TorvinDbContext context,
    NotificationWriter writer,
    RetryPolicy retry,
    ILogger<TransactionCompletedHandler> logger) : IMessageHandler
{
    public async Task<bool> HandleAsync(ReadOnlyMemory<byte> body, string? messageId, CancellationToken cancellationToken)
    {
        var message = MessageJson.Read<TransactionCompleted>(body, messageId, logger);
        if (message is null)
        {
            return false;
        }

        return await retry.RunAsync(messageId, "Notifikacija o transakciji", async ct =>
        {
            // Prenos uvijek ima obje kartice (kartica se ne briše, nego se zatvara)
            var transaction = await context.Transactions.AsNoTracking()
                .Where(t => t.Id == message.TransactionId)
                .Select(t => new
                {
                    t.SenderUserId,
                    t.ReceiverUserId,
                    SenderName = t.SenderUser.FirstName + " " + t.SenderUser.LastName,
                    ReceiverName = t.ReceiverUser.FirstName + " " + t.ReceiverUser.LastName,
                    SenderCardLast4 = t.SenderCard!.Last4Digits,
                    ReceiverCardLast4 = t.ReceiverCard!.Last4Digits,
                })
                .FirstOrDefaultAsync(ct);
            if (transaction is null)
            {
                logger.LogError("Poruka {MessageId}: transakcija {TransactionId} ne postoji, poruka ide u red neisporučenih.",
                    messageId, message.TransactionId);
                return false;
            }

            var amount = NotificationTemplates.Amount(message.Amount, message.CurrencyCode);
            NotificationDraft[] drafts =
            [
                NotificationTemplates.TransactionReceived(transaction.ReceiverUserId, message.TransactionId, amount,
                    transaction.ReceiverCardLast4, transaction.SenderName),
                NotificationTemplates.TransactionSent(transaction.SenderUserId, message.TransactionId, amount,
                    transaction.SenderCardLast4, transaction.ReceiverName),
            ];
            if (!await writer.AddOnceAsync(message.MessageId, drafts, ct))
            {
                logger.LogInformation("Poruka {MessageId} je već obrađena, notifikacije se ne ponavljaju.", messageId);
            }

            return true;
        }, _ => false, cancellationToken);
    }
}
