namespace Torvin.Worker.Consumers;

// Ponovni pokušaji obrade jedne poruke (upute, Dodatak A.1): pauze 1, 2, 4 i 8 s, ukupno 5 pokušaja, pa red
// neisporučenih. Pokušaj vraća true (potvrdi poruku) ili false (odmah u red neisporučenih, bez ponavljanja)
public sealed class RetryPolicy(ILogger<RetryPolicy> logger)
{
    public static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8),
    ];

    // Testovi zamjenjuju čekanje da ne traje 15 s
    public Func<TimeSpan, CancellationToken, Task> Delay { get; init; } = Task.Delay;

    // Trajna greška (isPermanent) se ne ponavlja. Njena poruka je naša, pa se smije logovati. Za ostale greške se
    // loguje samo tip, jer poruka spoljnog servisa (SMTP, baza) može nositi lične podatke
    public async Task<bool> RunAsync(string? messageId, string action, Func<CancellationToken, Task<bool>> attempt,
        Func<Exception, bool> isPermanent, CancellationToken cancellationToken)
    {
        for (var number = 1; ; number++)
        {
            try
            {
                // Kad pokušaj sam odluči da poruka ide u red neisporučenih, on loguje i razlog
                var acknowledged = await attempt(cancellationToken);
                if (acknowledged)
                {
                    logger.LogInformation("{Action}: poruka {MessageId} obrađena (pokušaj {Attempt}).", action, messageId, number);
                }

                return acknowledged;
            }
            catch (Exception ex) when (isPermanent(ex))
            {
                logger.LogError("{Action}: poruka {MessageId} je trajno odbijena i ide u red neisporučenih: {Reason}",
                    action, messageId, ex.Message);
                return false;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                if (number > RetryDelays.Length)
                {
                    logger.LogError("{Action}: poruka {MessageId} nije obrađena ni poslije {Attempts} pokušaja ({Error}) i ide u red neisporučenih.",
                        action, messageId, number, ex.GetType().Name);
                    return false;
                }

                var delay = RetryDelays[number - 1];
                logger.LogWarning("{Action}: poruka {MessageId} nije obrađena (pokušaj {Attempt}, {Error}). Novi pokušaj za {Delay} s.",
                    action, messageId, number, ex.GetType().Name, delay.TotalSeconds);
                await Delay(delay, cancellationToken);
            }
        }
    }
}
