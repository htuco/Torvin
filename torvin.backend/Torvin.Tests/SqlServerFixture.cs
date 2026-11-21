using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Testcontainers.MsSql;
using Torvin.Services.Database;

namespace Torvin.Tests;

// Pravi SQL Server u Dockeru, jedan za cijelu kolekciju. Baza u memoriji nema transakcije, RowVersion ni check
// constrainte, pa se atomičnost i istovremenost prenosa dokazuju ovdje. Bez upaljenog Dockera ovi testovi padaju
public sealed class SqlServerFixture : IAsyncLifetime
{
    // Isti image kao u docker-compose.yml
    private const string Image = "mcr.microsoft.com/mssql/server:2022-CU28-ubuntu-22.04";

    private readonly MsSqlContainer _container = new MsSqlBuilder(Image).Build();

    private string _connectionString = "";

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _connectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = "torvin_test",
        }.ConnectionString;

        // Migracije se primjenjuju kao pri startu API-ja, pa test usput provjerava i njih
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    // Isti interceptori kao u API-ju. Test može dodati svoj, npr. da obori snimanje u tačno određenom koraku
    public TorvinDbContext CreateContext(params IInterceptor[] extra) =>
        new(new DbContextOptionsBuilder<TorvinDbContext>()
            .UseSqlServer(_connectionString)
            .AddInterceptors([new SoftDeleteInterceptor(), new AuditFieldsInterceptor(), .. extra])
            .Options);
}

[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SqlServer";
}
