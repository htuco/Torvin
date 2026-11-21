using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Torvin.Model.Constants;
using Torvin.Model.Enums;
using Torvin.Model.Messages;
using Torvin.Services.Database;
using Torvin.Services.Security;
using Torvin.Services.Services;
using static Torvin.Tests.NotificationHandlerFixture;

namespace Torvin.Tests;

public class TransactionCompletedHandlerTests
{
    private sealed class FixedCurrentUser(int userId) : ICurrentUserService
    {
        public int UserId { get; } = userId;
        public string Role => AppRoles.User;
        public string TokenId => "test";
        public DateTime TokenExpiresAt => DateTime.UtcNow.AddHours(1);
    }

    // Pravi prenos kroz TransactionService, pa handler dobija tačno onu poruku koju API objavljuje
    private static async Task<(TorvinDbContext Context, TransactionCompleted Message)> TransferAsync()
    {
        var context = await NewContextAsync();
        var sender = await TestUsers.AddAsync(context, "posiljalac", AppRoles.User);
        var receiver = await TestUsers.AddAsync(context, "primalac", AppRoles.User);
        var from = await TestCards.AddAsync(context, sender, 100m, last4: "3020");
        await TestCards.AddAsync(context, receiver, 0m, last4: "8241");
        var publisher = new FakeMessagePublisher();
        var service = new TransactionService(context, new FixedCurrentUser(sender.Id), publisher,
            NullLogger<TransactionService>.Instance);

        await service.CreateAsync(new() { FromCardId = from.Id, ToUserId = receiver.Id, Amount = 25.5m });

        return (context, publisher.Published.Select(p => p.Message).OfType<TransactionCompleted>().Single());
    }

    [Fact]
    public async Task Transfer_NotifiesReceiverAndSender()
    {
        var (context, message) = await TransferAsync();

        Assert.True(await TransactionHandler(context).HandleAsync(Body(message), message.MessageId.ToString(), default));

        var notifications = await context.Notifications.AsNoTracking().OrderBy(n => n.Type).ToListAsync();
        Assert.Equal(2, notifications.Count);
        var received = notifications[0];
        Assert.Equal(NotificationType.TransactionReceived, received.Type);
        Assert.Equal(message.ToUserId, received.UserId);
        Assert.Equal("Primljena uplata", received.Title);
        Assert.Equal("Primili ste 25,50 BAM na karticu •••• 8241. Pošiljalac: Test posiljalac.", received.Message);
        var sent = notifications[1];
        Assert.Equal(NotificationType.TransactionSent, sent.Type);
        Assert.Equal(message.FromUserId, sent.UserId);
        Assert.Equal("Poslali ste 25,50 BAM s kartice •••• 3020. Primalac: Test primalac.", sent.Message);
        Assert.All(notifications, n => Assert.Equal(message.TransactionId, n.RelatedEntityId));
        Assert.All(notifications, n => Assert.False(n.IsRead));
    }

    [Fact]
    public async Task SameMessageTwice_IsAcknowledged_WithoutDuplicates()
    {
        var (context, message) = await TransferAsync();
        var handler = TransactionHandler(context);

        Assert.True(await handler.HandleAsync(Body(message), "id", default));
        Assert.True(await handler.HandleAsync(Body(message), "id", default));

        Assert.Equal(2, await context.Notifications.CountAsync());
    }

    [Fact]
    public async Task MissingTransaction_GoesToDeadLetter()
    {
        var context = await NewContextAsync();
        var message = new TransactionCompleted(Guid.NewGuid(), 9999, 1, 2, 10m, CurrencyCodes.BAM, null, DateTime.UtcNow);

        Assert.False(await TransactionHandler(context).HandleAsync(Body(message), "id", default));
        Assert.False(await context.Notifications.AnyAsync());
    }

    [Fact]
    public async Task InvalidJson_GoesToDeadLetter()
    {
        var context = await NewContextAsync();

        Assert.False(await TransactionHandler(context).HandleAsync(Invalid(), "id", default));
    }
}
