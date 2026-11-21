using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Torvin.Worker.Consumers;

// Zajednički dio svakog consumera: kanal na dijeljenoj konekciji, deklaracija reda, jedna poruka odjednom i ručna
// potvrda tek poslije obrade. Obrada vraća true (ack) ili false (nack bez vraćanja u red, pa poruka ide u *.dlq)
public abstract class QueueConsumer(RabbitMqConnectionProvider connections, ILogger logger) : BackgroundService
{
    protected abstract string Queue { get; }

    // Red (i exchange) deklarišu i API i Worker, pa nije bitno koji se prvi pokrene
    protected abstract Task DeclareAsync(IChannel channel, CancellationToken cancellationToken);

    protected abstract Task<bool> HandleAsync(ReadOnlyMemory<byte> body, string? messageId, CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connection = await connections.GetAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await DeclareAsync(channel, stoppingToken);

        // Jedna poruka odjednom: dok traju ponovni pokušaji, broker ne šalje sljedeću
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, delivery) =>
        {
            var messageId = delivery.BasicProperties.MessageId;
            if (await HandleAsync(delivery.Body, messageId, stoppingToken))
            {
                await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
            }
            else
            {
                await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(Queue, autoAck: false, consumer, stoppingToken);
        logger.LogInformation("Worker sluša red {Queue}.", Queue);

        // Kanal ostaje otvoren dok se Worker ne zaustavi. Nepotvrđena poruka se tada vraća u red
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
