using Microsoft.EntityFrameworkCore;
using Torvin.Services.Database;

namespace Torvin.Tests;

// Svaki test dobija svoju praznu bazu u memoriji, s istim interceptorima kao API
internal static class TestDatabase
{
    public static DbContextOptions<TorvinDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<TorvinDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new SoftDeleteInterceptor(), new AuditFieldsInterceptor())
            .Options;
}
