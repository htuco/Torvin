using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Torvin.Services.Database;

namespace Torvin.Tests;

// Svaki test dobija svoju praznu bazu u memoriji, s istim interceptorima kao API
internal static class TestDatabase
{
    public static DbContextOptions<TorvinDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<TorvinDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new SoftDeleteInterceptor(), new AuditFieldsInterceptor())
            // Baza u memoriji nema transakcije; seed ih otvara zbog SQL Servera
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
}
