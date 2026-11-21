using Microsoft.EntityFrameworkCore;
using Torvin.Model.Constants;
using Torvin.Model.Enums;
using Torvin.Services.Database;
using Torvin.Services.Notifications;

namespace Torvin.Tests;

public class NotificationWriterTests
{
    [Fact]
    public async Task SameMessage_IsWrittenOnlyOnce()
    {
        await using var context = new TorvinDbContext(TestDatabase.CreateOptions());
        await context.Database.EnsureCreatedAsync();
        var user = await TestUsers.AddAsync(context, "primalac", AppRoles.User);
        var writer = new NotificationWriter(context);
        var messageId = Guid.NewGuid();
        NotificationDraft[] drafts = [new(user.Id, NotificationType.System, "Naslov", "Tekst", 7)];

        Assert.True(await writer.AddOnceAsync(messageId, drafts));
        Assert.False(await writer.AddOnceAsync(messageId, drafts));

        var notification = await context.Notifications.AsNoTracking().SingleAsync();
        Assert.Equal(messageId, notification.SourceMessageId);
        Assert.False(notification.IsRead);
        Assert.Equal(7, notification.RelatedEntityId);
    }

    [Theory]
    [InlineData(25.5, "25,50 BAM")]
    [InlineData(1245, "1.245,00 BAM")]
    [InlineData(1234567.891, "1.234.567,89 BAM")]
    public void Amount_UsesBosnianFormat(double amount, string expected) =>
        Assert.Equal(expected, NotificationTemplates.Amount((decimal)amount, CurrencyCodes.BAM));
}
