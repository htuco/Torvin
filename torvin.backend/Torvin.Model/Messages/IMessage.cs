namespace Torvin.Model.Messages;

// Svaka poruka ima svoj Id. Isti Id je i u svojstvima RabbitMQ poruke, pa se logovi API-ja i Workera mogu povezati
public interface IMessage
{
    Guid MessageId { get; }
}
