using Microsoft.EntityFrameworkCore;
using Torvin.Services.Database;
using Torvin.Services.Database.Entities;

namespace Torvin.Services.Notifications;

// Upis notifikacija iz jedne poruke. Koristi ga samo Worker. Ista poruka ne upisuje ništa drugi put, pa je ponovna
// isporuka iz RabbitMQ-a (npr. poslije restarta Workera) bezopasna
public sealed class NotificationWriter(TorvinDbContext context)
{
    // Vraća false kad je poruka već obrađena. Sve notifikacije iz poruke idu u jednom snimanju: sve ili nijedna
    public async Task<bool> AddOnceAsync(Guid sourceMessageId, IReadOnlyList<NotificationDraft> drafts,
        CancellationToken cancellationToken = default)
    {
        if (await AlreadyWrittenAsync(sourceMessageId, cancellationToken))
        {
            return false;
        }

        foreach (var draft in drafts)
        {
            context.Notifications.Add(new Notification
            {
                UserId = draft.UserId,
                Type = draft.Type,
                Title = draft.Title,
                Message = draft.Message,
                RelatedEntityId = draft.RelatedEntityId,
                SourceMessageId = sourceMessageId,
            });
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // Dvije obrade iste poruke u isto vrijeme: drugu odbije unique indeks. Ako je poruka sada upisana, to je
            // duplikat, a inače je greška prava i ide u ponovni pokušaj
            context.ChangeTracker.Clear();
            if (await AlreadyWrittenAsync(sourceMessageId, cancellationToken))
            {
                return false;
            }

            throw;
        }
    }

    private Task<bool> AlreadyWrittenAsync(Guid sourceMessageId, CancellationToken cancellationToken) =>
        context.Notifications.AsNoTracking().AnyAsync(n => n.SourceMessageId == sourceMessageId, cancellationToken);
}
