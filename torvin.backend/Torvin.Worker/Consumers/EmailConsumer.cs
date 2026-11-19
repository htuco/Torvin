using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Torvin.Model.Messages;
using Torvin.Services.Configuration;
using Torvin.Services.Messaging;

namespace Torvin.Worker.Consumers;

// Sluša red "email" i šalje mailove koje je zatražio API. Poruka se potvrđuje ručno, tek poslije obrade
public sealed class EmailConsumer(
    IOptions<RabbitMqOptions> options,
    EmailMessageHandler handler,
    ILogger<EmailConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = RabbitMqConnectionFactory.Create(options.Value, "torvin-worker");
        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await RabbitMqTopology.DeclareEmailQueuesAsync(channel, stoppingToken);

        // Jedna poruka odjednom: dok traju ponovni pokušaji, broker ne šalje sljedeću
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, delivery) =>
        {
            var messageId = delivery.BasicProperties.MessageId;
            if (await handler.HandleAsync(delivery.Body, messageId, stoppingToken))
            {
                await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
            }
            else
            {
                await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(QueueNames.Email, autoAck: false, consumer, stoppingToken);
        logger.LogInformation("Worker sluša red {Queue}.", QueueNames.Email);

        // Konekcija ostaje otvorena dok se Worker ne zaustavi. Nepotvrđena poruka se tada vraća u red
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
