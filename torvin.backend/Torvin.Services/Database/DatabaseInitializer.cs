using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Torvin.Services.Seed;

namespace Torvin.Services.Database;

// API pri startu primijeni migracije i upiše demo podatke, da je za pokretanje dovoljan docker compose up
public sealed class DatabaseInitializer : IDatabaseInitializer
{
    public const int MaxAttempts = 10;

    private readonly TorvinDbContext _context;
    private readonly DataSeeder _seeder;
    private readonly ILogger<DatabaseInitializer> _logger;
    private readonly TimeSpan _retryDelay;

    public DatabaseInitializer(TorvinDbContext context, DataSeeder seeder, ILogger<DatabaseInitializer> logger)
        : this(context, seeder, logger, TimeSpan.FromSeconds(3))
    {
    }

    // Testovi zadaju kraću pauzu, da ne čekaju 27 sekundi
    internal DatabaseInitializer(TorvinDbContext context, DataSeeder seeder, ILogger<DatabaseInitializer> logger, TimeSpan retryDelay)
    {
        _context = context;
        _seeder = seeder;
        _logger = logger;
        _retryDelay = retryDelay;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await _context.Database.MigrateAsync(cancellationToken);
                _logger.LogInformation("Migracije baze su primijenjene.");
                break;
            }
            // Baza zna biti nedostupna prvih sekundi poslije starta kontejnera. Poslije zadnjeg pokušaja greška ide dalje i API ne starta
            catch (Exception exception) when (attempt < MaxAttempts && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Baza nije dostupna (pokušaj {Attempt}/{MaxAttempts}): {Error} Novi pokušaj za {Delay} s.",
                    attempt, MaxAttempts, exception.Message, _retryDelay.TotalSeconds);
                await Task.Delay(_retryDelay, cancellationToken);
            }
        }

        // Seed nije u petlji: njegova greška nije nedostupna baza, pa ponovni pokušaj ne bi pomogao
        await _seeder.SeedAsync(cancellationToken);
    }
}
