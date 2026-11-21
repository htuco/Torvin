using System.Text.Json;

namespace Torvin.Worker.Consumers;

// Čitanje tijela poruke. Neispravna poruka se ne može popraviti ponavljanjem, pa pozivalac vraća false (red neisporučenih)
public static class MessageJson
{
    public static T? Read<T>(ReadOnlyMemory<byte> body, string? messageId, ILogger logger) where T : class
    {
        try
        {
            var message = JsonSerializer.Deserialize<T>(body.Span);
            if (message is null)
            {
                logger.LogError("Poruka {MessageId} je prazna i ide u red neisporučenih.", messageId);
            }

            return message;
        }
        catch (JsonException ex)
        {
            logger.LogError("Poruka {MessageId} nije ispravan JSON i ide u red neisporučenih: {Reason}", messageId, ex.Message);
            return null;
        }
    }
}
