using RabbitMQ.Client;
using Torvin.Model.Messages;
using Torvin.Services.Messaging;

namespace Torvin.Worker.Consumers;

// Red notifikacija vezan za fanout exchange transaction-completed
public sealed class TransactionCompletedConsumer(
    RabbitMqConnectionProvider connections,
    IServiceScopeFactory scopes,
    ILogger<TransactionCompletedConsumer> logger)
    : ScopedQueueConsumer<TransactionCompletedHandler>(connections, scopes, logger)
{
    protected override string Queue => QueueNames.TransactionNotifications;

    protected override Task DeclareAsync(IChannel channel, CancellationToken cancellationToken) =>
        RabbitMqTopology.DeclareTransactionCompletedAsync(channel, cancellationToken);
}
