using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Torvin.Services.Configuration;
using Torvin.Services.Messaging;

namespace Torvin.Worker.Consumers;

// Jedna konekcija za sve consumere Workera, a svaki consumer otvara svoj kanal. Otvara se pri prvom traženju
public sealed class RabbitMqConnectionProvider(IOptions<RabbitMqOptions> options) : IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> GetAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Konekcija s automatskim oporavkom ostaje ista i kad se broker restartuje
            _connection ??= await RabbitMqConnectionFactory.Create(options.Value, "torvin-worker")
                .CreateConnectionAsync(cancellationToken);
            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _lock.Dispose();
    }
}
