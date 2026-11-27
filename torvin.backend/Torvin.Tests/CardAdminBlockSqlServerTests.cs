using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Torvin.Model.Constants;
using Torvin.Model.Requests;
using Torvin.Model.SearchObjects;
using Torvin.Services.Database;
using Torvin.Services.Security;
using Torvin.Services.Services;

namespace Torvin.Tests;

// Oznaka "blokirao administrator" dolazi iz audit traga (rola onoga ko je blokirao), pa se upit provjerava i na
// pravom SQL Serveru, a ne samo u bazi u memoriji
[Collection(SqlServerCollection.Name)]
[Trait("Category", "SqlServer")]
public class CardAdminBlockSqlServerTests(SqlServerFixture database)
{
    private sealed class TestCurrentUser(int userId, string role) : ICurrentUserService
    {
        public int UserId { get; } = userId;
        public string Role { get; } = role;
        public string TokenId => "test";
        public DateTime TokenExpiresAt => DateTime.UtcNow.AddHours(1);
    }

    private static CardService Service(TorvinDbContext context, int userId, string role = AppRoles.User) =>
        new(context, new TestCurrentUser(userId, role), new EmailCodeService(context), new FakeMessagePublisher(),
            NullLogger<CardService>.Instance);

    [Fact]
    public async Task ListAndDetails_MarkOnlyTheCardBlockedByAdmin()
    {
        int ownerId, byAdminId, byOwnerId;
        await using (var context = database.CreateContext())
        {
            var owner = await TestUsers.AddAsync(context, $"blk.{Guid.NewGuid():N}"[..30], AppRoles.User);
            var admin = await TestUsers.AddAsync(context, $"adm.{Guid.NewGuid():N}"[..30], AppRoles.Admin);
            ownerId = owner.Id;
            byAdminId = (await TestCards.AddAsync(context, owner, 0m, last4: "1111")).Id;
            byOwnerId = (await TestCards.AddAsync(context, owner, 0m, last4: "2222")).Id;

            var blockedId = await context.CardStatuses.Where(s => s.Code == CardStatusCodes.Blocked)
                .Select(s => s.Id).SingleAsync();
            await Service(context, admin.Id, AppRoles.Admin).ChangeStatusAsync(byAdminId,
                new CardStatusChangeRequest { CardStatusId = blockedId, Reason = "Sumnjiva aktivnost." });
            await Service(context, owner.Id).BlockAsync(byOwnerId);
        }

        await using var check = database.CreateContext();
        var service = Service(check, ownerId);
        var page = await service.GetMineAsync(new CardSearchObject());

        Assert.True(page.Items.Single(c => c.Id == byAdminId).IsBlockedByAdmin);
        Assert.False(page.Items.Single(c => c.Id == byOwnerId).IsBlockedByAdmin);
        Assert.True((await service.GetMineByIdAsync(byAdminId)).IsBlockedByAdmin);
    }
}
