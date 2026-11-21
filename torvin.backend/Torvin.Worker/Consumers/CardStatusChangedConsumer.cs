using RabbitMQ.Client;
using Torvin.Model.Messages;
using Torvin.Services.Messaging;

namespace Torvin.Worker.Consumers;

// Red promjena statusa kartice (card-status)
public sealed class CardStatusChangedConsumer(
    RabbitMqConnectionProvider connections,
    IServiceScopeFactory scopes,
    ILogger<CardStatusChangedConsumer> logger)
    : ScopedQueueConsumer<CardStatusChangedHandler>(connections, scopes, logger)
{
    protected override string Queue => QueueNames.CardStatus;

    protected override Task DeclareAsync(IChannel channel, CancellationToken cancellationToken) =>
        RabbitMqTopology.DeclareCardStatusQueuesAsync(channel, cancellationToken);
}
