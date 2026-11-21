namespace Torvin.Worker.Consumers;

// Obrada jedne poruke bez zavisnosti od RabbitMQ kanala, da se može testirati bez brokera.
// true = poruka se potvrđuje (ack), false = ide u red neisporučenih
public interface IMessageHandler
{
    Task<bool> HandleAsync(ReadOnlyMemory<byte> body, string? messageId, CancellationToken cancellationToken);
}
