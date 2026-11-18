using Microsoft.EntityFrameworkCore;
using Torvin.Model.Enums;
using Torvin.Services.Database;
using Torvin.Services.Database.Entities;

namespace Torvin.Tests;

public class SoftDeleteTests
{
    private static Budget NewBudget() => new()
    {
        UserId = 1, TransactionCategoryId = 1, CurrencyId = 1, Year = 2026, Month = 11, LimitAmount = 300m,
    };

    [Fact]
    public async Task RemovedSoftDeletable_IsHiddenButRowStays()
    {
        var options = TestDatabase.CreateOptions();
        await using (var context = new TorvinDbContext(options))
        {
            var budget = NewBudget();
            context.Budgets.Add(budget);
            await context.SaveChangesAsync();
            context.Budgets.Remove(budget);
            await context.SaveChangesAsync();
        }

        await using var check = new TorvinDbContext(options);
        Assert.Empty(await check.Budgets.ToListAsync());
        var stored = Assert.Single(await check.Budgets.IgnoreQueryFilters().ToListAsync());
        Assert.True(stored.IsDeleted);
        Assert.NotNull(stored.DeletedAt);
        Assert.Equal(DateTimeKind.Utc, stored.DeletedAt.Value.Kind);
    }

    [Fact]
    public async Task RemoveByIdOnly_KeepsOtherColumns()
    {
        var options = TestDatabase.CreateOptions();
        int id;
        await using (var context = new TorvinDbContext(options))
        {
            var budget = NewBudget();
            context.Budgets.Add(budget);
            await context.SaveChangesAsync();
            id = budget.Id;
        }

        await using (var context = new TorvinDbContext(options))
        {
            context.Budgets.Remove(new Budget { Id = id });
            await context.SaveChangesAsync();
        }

        await using var check = new TorvinDbContext(options);
        var stored = await check.Budgets.IgnoreQueryFilters().SingleAsync(b => b.Id == id);
        Assert.True(stored.IsDeleted);
        Assert.Equal(300m, stored.LimitAmount);
        Assert.Equal(11, stored.Month);
    }

    [Fact]
    public async Task RemovedEntityWithoutSoftDelete_IsDeletedPhysically()
    {
        var options = TestDatabase.CreateOptions();
        await using (var context = new TorvinDbContext(options))
        {
            var notification = new Notification
            {
                UserId = 1, Type = NotificationType.System, Title = "Naslov", Message = "Poruka",
            };
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();
            context.Notifications.Remove(notification);
            await context.SaveChangesAsync();
        }

        await using var check = new TorvinDbContext(options);
        Assert.Empty(await check.Notifications.IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public void EverySoftDeletableEntity_HasQueryFilter()
    {
        using var context = new TorvinDbContext(TestDatabase.CreateOptions());

        var softDeletable = context.Model.GetEntityTypes()
            .Where(type => typeof(ISoftDeletable).IsAssignableFrom(type.ClrType))
            .ToList();

        Assert.NotEmpty(softDeletable);
        Assert.All(softDeletable, type => Assert.NotEmpty(type.GetDeclaredQueryFilters()));
    }
}
