using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Torvin.Model.Constants;
using Torvin.Model.Exceptions;
using Torvin.Model.Requests;
using Torvin.Services.Database;
using Torvin.Services.Security;
using Torvin.Services.Services;

namespace Torvin.Tests;

// Baza u memoriji ne provodi unique indeks, pa se pravilo duplikata nad filtriranim indeksom
// (UserId, RecipientUserId) WHERE IsDeleted = 0 provjerava na pravom SQL Serveru
[Collection(SqlServerCollection.Name)]
[Trait("Category", "SqlServer")]
public class FavoriteRecipientSqlServerTests(SqlServerFixture database)
{
    private sealed class TestCurrentUser(int userId) : ICurrentUserService
    {
        public int UserId { get; } = userId;
        public string Role => AppRoles.User;
        public string TokenId => "test";
        public DateTime TokenExpiresAt => DateTime.UtcNow.AddHours(1);
    }

    private static FavoriteRecipientService Service(TorvinDbContext context, int userId) =>
        new(context, new TestCurrentUser(userId), NullLogger<FavoriteRecipientService>.Instance);

    private async Task<(int MeId, int RecipientId)> AddUsersAsync()
    {
        await using var context = database.CreateContext();
        var me = await TestUsers.AddAsync(context, $"fav.{Guid.NewGuid():N}"[..30], AppRoles.User);
        var recipient = await TestUsers.AddAsync(context, $"fav.{Guid.NewGuid():N}"[..30], AppRoles.User);
        await TestCards.AddAsync(context, recipient, 0m);
        return (me.Id, recipient.Id);
    }

    // Oba zahtjeva prođu provjeru "već postoji", a drugi upis zaustavlja indeks i dobija istu poruku
    [Fact]
    public async Task ConcurrentAddOfSameRecipient_SavesOne_SecondGetsDuplicateMessage()
    {
        var (meId, recipientId) = await AddUsersAsync();
        var request = new FavoriteRecipientInsertRequest { RecipientUserId = recipientId };

        var competing = new BeforeFirstSave(async () =>
        {
            await using var other = database.CreateContext();
            await Service(other, meId).CreateAsync(request);
        });
        await using var context = database.CreateContext(competing);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => Service(context, meId).CreateAsync(request));

        Assert.Equal(FavoriteRecipientService.AlreadyFavorite, exception.Message);
        await using var check = database.CreateContext();
        Assert.Equal(1, await check.FavoriteRecipients.CountAsync(f => f.UserId == meId));
    }

    [Fact]
    public async Task AfterSoftDelete_SameRecipientCanBeAddedAgain()
    {
        var (meId, recipientId) = await AddUsersAsync();
        var request = new FavoriteRecipientInsertRequest { RecipientUserId = recipientId };

        await using (var context = database.CreateContext())
        {
            var first = await Service(context, meId).CreateAsync(request);
            await Service(context, meId).DeleteAsync(first.Id);
        }

        await using (var context = database.CreateContext())
        {
            await Service(context, meId).CreateAsync(request);
        }

        await using var check = database.CreateContext();
        var rows = await check.FavoriteRecipients.IgnoreQueryFilters().Where(f => f.UserId == meId).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.Single(rows, f => !f.IsDeleted);
    }
}
