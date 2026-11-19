using RabbitMQ.Client;
using Torvin.Model.Messages;

namespace Torvin.Services.Messaging;

// Redove deklarišu i API i Worker, pa nije bitno koji se prvi pokrene. Deklaracija istog reda je idempotentna
public static class RabbitMqTopology
{
    public static Task DeclareEmailQueuesAsync(IChannel channel, CancellationToken cancellationToken = default) =>
        DeclareWithDeadLetterAsync(channel, QueueNames.Email, QueueNames.EmailDeadLetter, cancellationToken);

    public static Task DeclareCardStatusQueuesAsync(IChannel channel, CancellationToken cancellationToken = default) =>
        DeclareWithDeadLetterAsync(channel, QueueNames.CardStatus, QueueNames.CardStatusDeadLetter, cancellationToken);

    private static async Task DeclareWithDeadLetterAsync(IChannel channel, string queue, string deadLetterQueue,
        CancellationToken cancellationToken)
    {
        await channel.QueueDeclareAsync(deadLetterQueue, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: cancellationToken);

        // Odbijena poruka (nack bez vraćanja u red) ide preko default exchangea u red neisporučenih
        var arguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = "",
            ["x-dead-letter-routing-key"] = deadLetterQueue,
        };
        await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false,
            arguments: arguments, cancellationToken: cancellationToken);
    }
}
