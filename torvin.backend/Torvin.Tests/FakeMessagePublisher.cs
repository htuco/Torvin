using System.Collections.Concurrent;
using Torvin.Model.Messages;
using Torvin.Services.Messaging;

namespace Torvin.Tests;

// Testovi nemaju RabbitMQ: poruke se pamte, a test može u trenutku objave provjeriti stanje baze
public sealed class FakeMessagePublisher : IMessagePublisher
{
    private readonly ConcurrentQueue<(string Queue, IMessage Message)> _published = new();

    public Func<IMessage, Task>? OnPublish { get; set; }

    public IReadOnlyList<(string Queue, IMessage Message)> Published => [.. _published];

    public async Task PublishAsync<T>(string queue, T message, CancellationToken cancellationToken = default)
        where T : IMessage
    {
        if (OnPublish is not null)
        {
            await OnPublish(message);
        }

        _published.Enqueue((queue, message));
    }
}
