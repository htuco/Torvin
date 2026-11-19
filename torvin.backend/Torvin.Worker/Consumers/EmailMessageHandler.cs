using System.Text.Json;
using Torvin.Model.Messages;
using Torvin.Worker.Email;

namespace Torvin.Worker.Consumers;

// Obrada jedne poruke, odvojena od RabbitMQ kanala da se može testirati bez brokera.
// Vraća true kad se poruka potvrđuje (ack), a false kad ide u red neisporučenih (nack bez vraćanja u red)
public sealed class EmailMessageHandler(IEmailSender sender, ILogger<EmailMessageHandler> logger)
{
    // Pauze prije ponovnih pokušaja (upute, Dodatak A.1): ukupno 5 pokušaja slanja
    public static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8),
    ];

    // Testovi zamjenjuju čekanje da ne traje 15 s
    public Func<TimeSpan, CancellationToken, Task> Delay { get; init; } = Task.Delay;

    public async Task<bool> HandleAsync(ReadOnlyMemory<byte> body, string? messageId, CancellationToken cancellationToken)
    {
        EmailRequested? email;
        try
        {
            email = JsonSerializer.Deserialize<EmailRequested>(body.Span);
        }
        catch (JsonException ex)
        {
            logger.LogError("Poruka {MessageId} nije ispravan JSON i ide u red neisporučenih: {Reason}", messageId, ex.Message);
            return false;
        }

        if (email is null || string.IsNullOrWhiteSpace(email.To))
        {
            logger.LogError("Poruka {MessageId} nema primaoca i ide u red neisporučenih.", messageId);
            return false;
        }

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await sender.SendAsync(email.To, email.Subject, email.Body, cancellationToken);
                logger.LogInformation("Mail iz poruke {MessageId} je poslan (pokušaj {Attempt}).", messageId, attempt);
                return true;
            }
            // Poruka trajne greške je naša (bez adrese primaoca), pa se smije logovati
            catch (EmailRejectedException ex)
            {
                logger.LogError("Mail iz poruke {MessageId} je trajno odbijen i ide u red neisporučenih: {Reason}",
                    messageId, ex.Message);
                return false;
            }
            // Loguje se samo tip greške: poruka SMTP servera može sadržati adresu primaoca
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt > RetryDelays.Length)
                {
                    logger.LogError("Mail iz poruke {MessageId} nije poslan ni poslije {Attempts} pokušaja ({Error}) i ide u red neisporučenih.",
                        messageId, attempt, ex.GetType().Name);
                    return false;
                }

                var delay = RetryDelays[attempt - 1];
                logger.LogWarning("Slanje maila iz poruke {MessageId} nije uspjelo (pokušaj {Attempt}, {Error}). Novi pokušaj za {Delay} s.",
                    messageId, attempt, ex.GetType().Name, delay.TotalSeconds);
                await Delay(delay, cancellationToken);
            }
        }
    }
}
