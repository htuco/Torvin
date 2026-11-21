extern alias worker;

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Torvin.Services.Database;
using Torvin.Services.Notifications;
using worker::Torvin.Worker.Consumers;

namespace Torvin.Tests;

// Baza u memoriji i handleri Workera kakve ih pravi DI, s ponovnim pokušajima bez čekanja
internal static class NotificationHandlerFixture
{
    public static async Task<TorvinDbContext> NewContextAsync()
    {
        var context = new TorvinDbContext(TestDatabase.CreateOptions());
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    public static RetryPolicy Retry() =>
        new(NullLogger<RetryPolicy>.Instance) { Delay = (_, _) => Task.CompletedTask };

    public static TransactionCompletedHandler TransactionHandler(TorvinDbContext context) =>
        new(context, new NotificationWriter(context), Retry(), NullLogger<TransactionCompletedHandler>.Instance);

    public static CardStatusChangedHandler CardHandler(TorvinDbContext context) =>
        new(context, new NotificationWriter(context), Retry(), NullLogger<CardStatusChangedHandler>.Instance);

    // Isto tijelo koje šalje RabbitMqPublisher
    public static ReadOnlyMemory<byte> Body<T>(T message) => JsonSerializer.SerializeToUtf8Bytes(message);

    public static ReadOnlyMemory<byte> Invalid() => Encoding.UTF8.GetBytes("nije json");
}
