using RabbitMQ.Client;
using Torvin.Model.Messages;
using Torvin.Services.Messaging;

namespace Torvin.Worker.Consumers;

// Sluša red "email" i šalje mailove koje je zatražio API
public sealed class EmailConsumer(
    RabbitMqConnectionProvider connections,
    EmailMessageHandler handler,
    ILogger<EmailConsumer> logger) : QueueConsumer(connections, logger)
{
    protected override string Queue => QueueNames.Email;

    protected override Task DeclareAsync(IChannel channel, CancellationToken cancellationToken) =>
        RabbitMqTopology.DeclareEmailQueuesAsync(channel, cancellationToken);

    protected override Task<bool> HandleAsync(ReadOnlyMemory<byte> body, string? messageId, CancellationToken cancellationToken) =>
        handler.HandleAsync(body, messageId, cancellationToken);
}
