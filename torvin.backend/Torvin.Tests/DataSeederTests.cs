using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Torvin.Model.Constants;
using Torvin.Model.Enums;
using Torvin.Services.Database;
using Torvin.Services.Seed;
using Torvin.Services.Security;

namespace Torvin.Tests;

// Seed se izvrši jednom za sve testove (hashiranje lozinki traje), a svaki test provjerava jedno pravilo
public sealed class SeededDatabase : IAsyncLifetime
{
    public DbContextOptions<TorvinDbContext> Options { get; } = TestDatabase.CreateOptions();

    public async Task InitializeAsync()
    {
        await using var context = new TorvinDbContext(Options);
        await context.Database.EnsureCreatedAsync();
        await new DataSeeder(context, NullLogger<DataSeeder>.Instance).SeedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

public class DataSeederTests(SeededDatabase database) : IClassFixture<SeededDatabase>
{
    private TorvinDbContext NewContext() => new(database.Options);

    [Fact]
    public async Task DemoUsers_ExistWithRolesAndPassword()
    {
        await using var context = NewContext();

        var desktop = await context.Users.Include(u => u.Role).SingleAsync(u => u.Username == "desktop");
        var mobile = await context.Users.Include(u => u.Role).SingleAsync(u => u.Username == "mobile");
        var mobilePassword = await context.UserPasswords.SingleAsync(p => p.UserId == mobile.Id);

        Assert.Equal(AppRoles.Admin, desktop.Role.Name);
        Assert.Equal(AppRoles.User, mobile.Role.Name);
        Assert.True(PasswordHasher.Verify(DataSeeder.DemoPassword, mobilePassword.PasswordHash));
        Assert.True(await context.Users.CountAsync() >= 7);
    }

    [Fact]
    public async Task Transactions_CoverThreeMonthsAndAllCategories()
    {
        await using var context = NewContext();
        var transactions = await context.Transactions.ToListAsync();

        Assert.True(transactions.Count >= 50);
        Assert.True(transactions.Select(t => (t.CreatedAt.Year, t.CreatedAt.Month)).Distinct().Count() >= 3);
        Assert.Equal(
            await context.TransactionCategories.CountAsync(),
            transactions.Where(t => t.TransactionCategoryId is not null).Select(t => t.TransactionCategoryId).Distinct().Count());
        Assert.All(transactions, t => Assert.True(t.CreatedAt <= DateTime.UtcNow));
    }

    [Fact]
    public async Task MobileMainCard_BalanceMatchesHistory()
    {
        await using var context = NewContext();
        var card = await context.Cards.SingleAsync(c => c.User.Username == "mobile" && c.IsDefault);
        var moved = await context.Transactions
            .Where(t => t.TransactionStatus.Code != TransactionStatusCodes.Failed)
            .Where(t => t.SenderCardId == card.Id || t.ReceiverCardId == card.Id)
            .ToListAsync();
        var savings = await context.SavingsMovements.IgnoreQueryFilters().Where(m => m.CardId == card.Id).ToListAsync();

        var expected = DataSeeder.MobileMainInitialBalance
            + moved.Where(t => t.ReceiverCardId == card.Id).Sum(t => t.Amount)
            - moved.Where(t => t.SenderCardId == card.Id).Sum(t => t.Amount)
            - savings.Sum(m => m.Type == SavingsMovementType.Deposit ? m.Amount : -m.Amount);

        Assert.Equal(expected, card.Balance);
    }

    [Fact]
    public async Task Cards_AreNotNegativeAndCoverAllStatusesAndTypes()
    {
        await using var context = NewContext();
        var cards = await context.Cards.ToListAsync();

        Assert.All(cards, c => Assert.True(c.Balance >= 0));
        Assert.Equal(await context.CardStatuses.CountAsync(), cards.Select(c => c.CardStatusId).Distinct().Count());
        Assert.Equal(await context.CardTypes.CountAsync(), cards.Select(c => c.CardTypeId).Distinct().Count());
    }

    [Fact]
    public async Task Requests_ExistInEveryState()
    {
        await using var context = NewContext();

        Assert.Equal(Enum.GetValues<MoneyRequestStatus>().Length,
            await context.MoneyRequests.Select(r => r.Status).Distinct().CountAsync());
        Assert.Equal(Enum.GetValues<ReversalRequestStatus>().Length,
            await context.ReversalRequests.Select(r => r.Status).Distinct().CountAsync());
        Assert.Equal(Enum.GetValues<StandingOrderStatus>().Length,
            await context.StandingOrders.Select(o => o.Status).Distinct().CountAsync());
        // Anomaliju i njenu notifikaciju upisuje detektor anomalija, pa ih seed nema
        Assert.Equal(Enum.GetValues<NotificationType>().Length - 1,
            await context.Notifications.Select(n => n.Type).Distinct().CountAsync());
        Assert.Equal(await context.TransactionStatuses.CountAsync(),
            await context.Transactions.Select(t => t.TransactionStatusId).Distinct().CountAsync());
    }

    [Fact]
    public async Task ApprovedReversal_HasCorrectiveTransaction()
    {
        await using var context = NewContext();
        var approved = await context.ReversalRequests
            .Include(r => r.Transaction).ThenInclude(t => t.TransactionStatus)
            .Include(r => r.CorrectiveTransaction)
            .Where(r => r.Status == ReversalRequestStatus.Approved)
            .ToListAsync();

        Assert.NotEmpty(approved);
        Assert.All(approved, r =>
        {
            Assert.Equal(TransactionStatusCodes.Reversed, r.Transaction.TransactionStatus.Code);
            Assert.Equal(r.Transaction.Amount, r.CorrectiveTransaction!.Amount);
            Assert.Equal(r.TransactionId, r.CorrectiveTransaction.OriginalTransactionId);
        });
    }

    [Fact]
    public async Task SavingsGoal_CurrentAmountIsSumOfMovements()
    {
        await using var context = NewContext();
        var goals = await context.SavingsGoals.Include(g => g.Movements).ToListAsync();

        Assert.True(goals.Count >= 4);
        Assert.All(goals, g => Assert.Equal(
            g.Movements.Sum(m => m.Type == SavingsMovementType.Deposit ? m.Amount : -m.Amount), g.CurrentAmount));
    }

    [Fact]
    public async Task NewsAndAvatars_HaveImages()
    {
        await using var context = NewContext();

        var news = await context.News.ToListAsync();
        Assert.True(news.Count >= 5);
        Assert.All(news, n => Assert.True(n.Image.Length > 0));
        Assert.True(await context.Users.CountAsync(u => u.ProfileImage != null) >= 3);
    }

    [Fact]
    public async Task SecondRun_DoesNotDuplicate()
    {
        await using var context = NewContext();
        var usersBefore = await context.Users.CountAsync();

        await new DataSeeder(context, NullLogger<DataSeeder>.Instance).SeedAsync();

        Assert.Equal(usersBefore, await context.Users.CountAsync());
    }
}
