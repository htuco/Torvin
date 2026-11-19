using Torvin.Model.Messages;

namespace Torvin.Services.Messaging;

// Servisi objavljuju poruku tek poslije uspješnog SaveChangesAsync, da Worker ne radi s podacima kojih nema
public interface IMessagePublisher
{
    Task PublishAsync<T>(string queue, T message, CancellationToken cancellationToken = default) where T : IMessage;
}
