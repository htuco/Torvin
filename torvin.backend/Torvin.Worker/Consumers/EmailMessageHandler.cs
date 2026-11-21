using Torvin.Model.Messages;
using Torvin.Worker.Email;

namespace Torvin.Worker.Consumers;

// Obrada jedne poruke, odvojena od RabbitMQ kanala da se može testirati bez brokera.
// Vraća true kad se poruka potvrđuje (ack), a false kad ide u red neisporučenih (nack bez vraćanja u red)
public sealed class EmailMessageHandler(IEmailSender sender, RetryPolicy retry, ILogger<EmailMessageHandler> logger)
{
    public async Task<bool> HandleAsync(ReadOnlyMemory<byte> body, string? messageId, CancellationToken cancellationToken)
    {
        var email = MessageJson.Read<EmailRequested>(body, messageId, logger);
        if (email is null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(email.To))
        {
            logger.LogError("Poruka {MessageId} nema primaoca i ide u red neisporučenih.", messageId);
            return false;
        }

        // Odbijena adresa (EmailRejectedException) se ne ponavlja, a ostale greške SMTP-a idu u ponovni pokušaj
        return await retry.RunAsync(messageId, "Slanje maila", async ct =>
        {
            await sender.SendAsync(email.To, email.Subject, email.Body, ct);
            return true;
        }, ex => ex is EmailRejectedException, cancellationToken);
    }
}
