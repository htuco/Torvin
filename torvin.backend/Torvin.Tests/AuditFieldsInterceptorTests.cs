using Microsoft.EntityFrameworkCore;
using Torvin.Services.Database;
using Torvin.Services.Database.Entities;

namespace Torvin.Tests;

public class AuditFieldsInterceptorTests
{
    private static TransactionCategory NewCategory() => new() { Name = "Putovanja" };

    [Fact]
    public async Task AddedEntity_GetsCreatedAndModifiedAtInUtc()
    {
        await using var context = new TorvinDbContext(TestDatabase.CreateOptions());
        var before = DateTime.UtcNow;

        var category = NewCategory();
        context.TransactionCategories.Add(category);
        await context.SaveChangesAsync();

        Assert.Equal(DateTimeKind.Utc, category.CreatedAt.Kind);
        Assert.InRange(category.CreatedAt, before, DateTime.UtcNow);
        Assert.Equal(category.CreatedAt, category.ModifiedAt);
    }

    [Fact]
    public async Task ModifiedEntity_UpdatesModifiedAtAndKeepsCreatedAt()
    {
        var options = TestDatabase.CreateOptions();
        int id;
        DateTime createdAt;
        await using (var context = new TorvinDbContext(options))
        {
            var category = NewCategory();
            context.TransactionCategories.Add(category);
            await context.SaveChangesAsync();
            (id, createdAt) = (category.Id, category.CreatedAt);
        }

        await using (var context = new TorvinDbContext(options))
        {
            var category = await context.TransactionCategories.SingleAsync(c => c.Id == id);
            category.Description = "Avion, hotel i smještaj.";
            // Servis ne može slučajno pregaziti vrijeme nastanka
            category.CreatedAt = DateTime.UtcNow.AddYears(-5);
            await context.SaveChangesAsync();
        }

        await using var check = new TorvinDbContext(options);
        var stored = await check.TransactionCategories.SingleAsync(c => c.Id == id);
        Assert.Equal(createdAt, stored.CreatedAt);
        Assert.True(stored.ModifiedAt >= createdAt);
    }

    [Fact]
    public async Task ReadDateTimes_AreUtc()
    {
        var options = TestDatabase.CreateOptions();
        var unspecified = new DateTime(2026, 11, 18, 10, 0, 0, DateTimeKind.Unspecified);
        int id;
        await using (var context = new TorvinDbContext(options))
        {
            var goal = new SavingsGoal
            {
                UserId = 1, CurrencyId = 1, Name = "Ljetovanje", TargetAmount = 1000m, AchievedAt = unspecified,
            };
            context.SavingsGoals.Add(goal);
            await context.SaveChangesAsync();
            id = goal.Id;
        }

        await using var check = new TorvinDbContext(options);
        var stored = await check.SavingsGoals.SingleAsync(g => g.Id == id);
        Assert.Equal(DateTimeKind.Utc, stored.CreatedAt.Kind);
        Assert.Equal(DateTimeKind.Utc, stored.AchievedAt!.Value.Kind);
        Assert.Equal(unspecified.Ticks, stored.AchievedAt.Value.Ticks);
    }
}
