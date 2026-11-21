using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Torvin.Model.Messages;
using Torvin.Services.Configuration;
using Torvin.Services.Messaging;

namespace Torvin.API.Messaging;

// Singleton s jednom konekcijom za cijeli API. Konekcija se otvara tek pri prvoj objavi, pa API (i testovi)
// mogu startati i bez RabbitMQ-a. Kanal je kratkotrajan i otvara se po objavi
public sealed class RabbitMqPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqPublisher> logger)
    : IMessagePublisher, IAsyncDisposable
{
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnection? _connection;

    public Task PublishAsync<T>(string queue, T message, CancellationToken cancellationToken = default)
        where T : IMessage =>
        PublishCoreAsync(exchange: "", routingKey: queue, message, cancellationToken);

    // Fanout exchange ne gleda routing key, pa je prazan
    public Task PublishEventAsync<T>(string exchange, T message, CancellationToken cancellationToken = default)
        where T : IMessage =>
        PublishCoreAsync(exchange, routingKey: "", message, cancellationToken);

    private async Task PublishCoreAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken)
        where T : IMessage
    {
        var connection = await GetConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        var messageId = message.MessageId.ToString();
        var properties = new BasicProperties
        {
            Persistent = true,
            MessageId = messageId,
            ContentType = "application/json",
        };
        var body = JsonSerializer.SerializeToUtf8Bytes(message);

        await channel.BasicPublishAsync(exchange: exchange, routingKey: routingKey, mandatory: false,
            basicProperties: properties, body: body, cancellationToken: cancellationToken);

        // Tijelo se ne loguje, jer nosi kod za potvrdu i adresu primaoca
        logger.LogInformation("Poruka {MessageId} je objavljena u {Destination}.", messageId,
            exchange.Length == 0 ? routingKey : exchange);
    }

    private async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
            }

            var factory = RabbitMqConnectionFactory.Create(options.Value, "torvin-api");
            _connection = await factory.CreateConnectionAsync(cancellationToken);

            // Redovi postoje prije prve objave, i kad Worker još nije startao
            await using var channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await RabbitMqTopology.DeclareEmailQueuesAsync(channel, cancellationToken);
            await RabbitMqTopology.DeclareCardStatusQueuesAsync(channel, cancellationToken);
            await RabbitMqTopology.DeclareTransactionCompletedAsync(channel, cancellationToken);
            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _connectionLock.Dispose();
    }
}
