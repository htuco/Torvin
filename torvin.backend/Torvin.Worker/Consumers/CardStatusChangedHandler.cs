using Microsoft.EntityFrameworkCore;
using Torvin.Model.Constants;
using Torvin.Model.Messages;
using Torvin.Services.Database;
using Torvin.Services.Notifications;

namespace Torvin.Worker.Consumers;

// Promjena statusa kartice: vlasnik dobija notifikaciju kad je kartica blokirana, izgubljena ili ukradena, i za svaku
// promjenu koju je uradio administrator. Aktivaciju i deblokiranje vlasnik je upravo sam uradio, pa za njih nema poruke
public sealed class CardStatusChangedHandler(
    TorvinDbContext context,
    NotificationWriter writer,
    RetryPolicy retry,
    ILogger<CardStatusChangedHandler> logger) : IMessageHandler
{
    private static readonly string[] AlwaysNotified = [CardStatusCodes.Blocked, CardStatusCodes.Lost, CardStatusCodes.Stolen];

    public async Task<bool> HandleAsync(ReadOnlyMemory<byte> body, string? messageId, CancellationToken cancellationToken)
    {
        var message = MessageJson.Read<CardStatusChanged>(body, messageId, logger);
        if (message is null)
        {
            return false;
        }

        return await retry.RunAsync(messageId, "Notifikacija o kartici", async ct =>
        {
            var card = await context.Cards.AsNoTracking()
                .Where(c => c.Id == message.CardId)
                .Select(c => new { c.UserId, c.Last4Digits })
                .FirstOrDefaultAsync(ct);
            if (card is null)
            {
                logger.LogError("Poruka {MessageId}: kartica {CardId} ne postoji, poruka ide u red neisporučenih.",
                    messageId, message.CardId);
                return false;
            }

            var changedByAdmin = await context.Users.AnyAsync(u => u.Id == message.ChangedByUserId
                && u.Role.Name == AppRoles.Admin, ct);
            if (!changedByAdmin && !AlwaysNotified.Contains(message.NewStatusCode))
            {
                logger.LogInformation("Poruka {MessageId}: prelaz u {Status} od vlasnika ne pravi notifikaciju.",
                    messageId, message.NewStatusCode);
                return true;
            }

            var statusName = await context.CardStatuses.AsNoTracking()
                .Where(s => s.Code == message.NewStatusCode)
                .Select(s => s.Name)
                .FirstOrDefaultAsync(ct) ?? message.NewStatusCode;
            var draft = NotificationTemplates.CardStatusChanged(card.UserId, message.CardId, card.Last4Digits,
                message.NewStatusCode, statusName, changedByAdmin, message.Reason);
            if (!await writer.AddOnceAsync(message.MessageId, [draft], ct))
            {
                logger.LogInformation("Poruka {MessageId} je već obrađena, notifikacija se ne ponavlja.", messageId);
            }

            return true;
        }, _ => false, cancellationToken);
    }
}
