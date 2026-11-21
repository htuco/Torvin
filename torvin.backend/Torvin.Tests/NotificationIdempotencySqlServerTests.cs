using Microsoft.EntityFrameworkCore;
using Torvin.Model.Constants;
using Torvin.Model.Enums;
using Torvin.Services.Notifications;

namespace Torvin.Tests;

// Dvije obrade iste poruke u isto vrijeme (npr. poruka isporučena dvaput dok prva obrada još traje). Obje prođu
// provjeru "već postoji", pa duplikat zaustavlja tek unique indeks (SourceMessageId, UserId) na pravom SQL Serveru
[Collection(SqlServerCollection.Name)]
[Trait("Category", "SqlServer")]
public class NotificationIdempotencySqlServerTests(SqlServerFixture database)
{
    [Fact]
    public async Task ConcurrentProcessingOfSameMessage_WritesOneNotification()
    {
        int userId;
        await using (var setup = database.CreateContext())
        {
            userId = (await TestUsers.AddAsync(setup, $"notif.{Guid.NewGuid():N}"[..30], AppRoles.User)).Id;
        }

        var messageId = Guid.NewGuid();
        NotificationDraft[] drafts = [new(userId, NotificationType.CardBlocked, "Kartica blokirana", "Tekst", 1)];
        var secondWritten = false;

        var competing = new BeforeFirstSave(async () =>
        {
            await using var other = database.CreateContext();
            secondWritten = await new NotificationWriter(other).AddOnceAsync(messageId, drafts);
        });
        await using var context = database.CreateContext(competing);
        var firstWritten = await new NotificationWriter(context).AddOnceAsync(messageId, drafts);

        Assert.True(secondWritten);
        Assert.False(firstWritten);
        await using var check = database.CreateContext();
        Assert.Equal(1, await check.Notifications.CountAsync(n => n.SourceMessageId == messageId));
    }
}
