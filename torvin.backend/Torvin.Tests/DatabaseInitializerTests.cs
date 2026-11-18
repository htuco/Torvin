using Microsoft.Extensions.Logging;
using Torvin.Services.Database;

namespace Torvin.Tests;

public class DatabaseInitializerTests
{
    // Pamti upozorenja, da test vidi koliko je puta inicijalizator pokušao
    private sealed class WarningCounter<T> : ILogger<T>
    {
        public int Warnings { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                Warnings++;
            }
        }
    }

    [Fact]
    public async Task UnavailableDatabase_RetriesThenFails()
    {
        // Baza u memoriji ne podržava migracije, pa svaki pokušaj padne kao nedostupna baza
        await using var context = new TorvinDbContext(TestDatabase.CreateOptions());
        var logger = new WarningCounter<DatabaseInitializer>();
        var initializer = new DatabaseInitializer(context, logger, TimeSpan.Zero);

        await Assert.ThrowsAnyAsync<Exception>(() => initializer.InitializeAsync());

        Assert.Equal(DatabaseInitializer.MaxAttempts - 1, logger.Warnings);
    }
}
