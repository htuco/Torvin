namespace Torvin.Worker.Consumers;

// Consumer čiji handler radi s bazom: svaka poruka dobija svoj scope, pa i svoj DbContext
public abstract class ScopedQueueConsumer<THandler>(
    RabbitMqConnectionProvider connections,
    IServiceScopeFactory scopes,
    ILogger logger) : QueueConsumer(connections, logger) where THandler : IMessageHandler
{
    protected override async Task<bool> HandleAsync(ReadOnlyMemory<byte> body, string? messageId,
        CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<THandler>().HandleAsync(body, messageId, cancellationToken);
    }
}
