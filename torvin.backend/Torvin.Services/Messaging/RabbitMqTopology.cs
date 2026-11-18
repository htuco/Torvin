using RabbitMQ.Client;
using Torvin.Model.Messages;

namespace Torvin.Services.Messaging;

// Redove deklarišu i API i Worker, pa nije bitno koji se prvi pokrene. Deklaracija istog reda je idempotentna
public static class RabbitMqTopology
{
    public static async Task DeclareEmailQueuesAsync(IChannel channel, CancellationToken cancellationToken = default)
    {
        await channel.QueueDeclareAsync(QueueNames.EmailDeadLetter, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: cancellationToken);

        // Odbijena poruka (nack bez vraćanja u red) ide preko default exchangea u red neisporučenih
        var arguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = "",
            ["x-dead-letter-routing-key"] = QueueNames.EmailDeadLetter,
        };
        await channel.QueueDeclareAsync(QueueNames.Email, durable: true, exclusive: false, autoDelete: false,
            arguments: arguments, cancellationToken: cancellationToken);
    }
}
