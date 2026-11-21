using Torvin.Model.Messages;

namespace Torvin.Services.Messaging;

// Servisi objavljuju poruku tek poslije uspješnog SaveChangesAsync, da Worker ne radi s podacima kojih nema
public interface IMessagePublisher
{
    // Poruka za tačno jedan red (jedan consumer)
    Task PublishAsync<T>(string queue, T message, CancellationToken cancellationToken = default) where T : IMessage;

    // Događaj na fanout exchange: kopiju dobija svaki red vezan za njega
    Task PublishEventAsync<T>(string exchange, T message, CancellationToken cancellationToken = default) where T : IMessage;
}
