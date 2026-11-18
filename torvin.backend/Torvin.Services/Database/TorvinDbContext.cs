using Microsoft.EntityFrameworkCore;
using Torvin.Services.Database.Entities;

namespace Torvin.Services.Database;

public class TorvinDbContext(DbContextOptions<TorvinDbContext> options) : DbContext(options)
{
    // Referentni podaci
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<CardType> CardTypes => Set<CardType>();
    public DbSet<CardStatus> CardStatuses => Set<CardStatus>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<TransactionType> TransactionTypes => Set<TransactionType>();
    public DbSet<TransactionStatus> TransactionStatuses => Set<TransactionStatus>();
    public DbSet<TransactionCategory> TransactionCategories => Set<TransactionCategory>();

    // Glavne tabele
    public DbSet<User> Users => Set<User>();
    public DbSet<UserPassword> UserPasswords => Set<UserPassword>();
    public DbSet<EmailToken> EmailTokens => Set<EmailToken>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TransactionAnomaly> TransactionAnomalies => Set<TransactionAnomaly>();
    public DbSet<ReversalRequest> ReversalRequests => Set<ReversalRequest>();
    public DbSet<MoneyRequest> MoneyRequests => Set<MoneyRequest>();
    public DbSet<StandingOrder> StandingOrders => Set<StandingOrder>();
    public DbSet<StandingOrderExecution> StandingOrderExecutions => Set<StandingOrderExecution>();
    public DbSet<SavingsGoal> SavingsGoals => Set<SavingsGoal>();
    public DbSet<SavingsMovement> SavingsMovements => Set<SavingsMovement>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TorvinDbContext).Assembly);

    // SQL Server ne pamti vrstu vremena, pa bi pročitano vrijeme bilo Unspecified i JSON bi izašao bez "Z".
    // Sva vremena se upisuju kao UTC, pa se tako i čitaju
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }
}
