using Torvin.Model.Constants;
using Torvin.Model.Exceptions;
using Torvin.Model.Responses;
using Torvin.Model.SearchObjects;
using Torvin.Services.Database;
using Torvin.Services.Database.Entities;
using Torvin.Services.Security;
using Torvin.Services.Services;

namespace Torvin.Tests;

public class RecipientServiceTests
{
    private sealed class TestCurrentUser(int userId) : ICurrentUserService
    {
        public int UserId { get; } = userId;
        public string Role => AppRoles.User;
        public string TokenId => "test";
        public DateTime TokenExpiresAt => DateTime.UtcNow.AddHours(1);
    }

    private static async Task<(TorvinDbContext Context, RecipientService Service)> NewAsync()
    {
        var context = new TorvinDbContext(TestDatabase.CreateOptions());
        await context.Database.EnsureCreatedAsync();
        var me = await AddWithCardAsync(context, "marko.ja");
        return (context, new RecipientService(context, new TestCurrentUser(me.Id)));
    }

    private static async Task<User> AddWithCardAsync(TorvinDbContext context, string username, string role = AppRoles.User,
        bool isActive = true, bool emailConfirmed = true, string cardStatus = CardStatusCodes.Active)
    {
        var user = await TestUsers.AddAsync(context, username, role, isActive, emailConfirmed);
        await TestCards.AddAsync(context, user, 0m, cardStatus);
        return user;
    }

    private static Task<PagedResult<RecipientResponse>> SearchAsync(
        RecipientService service, string fts, int pageSize = 20) =>
        service.SearchAsync(new RecipientSearchObject { Fts = fts, PageSize = pageSize });

    [Theory]
    [InlineData(null)]
    [InlineData("ma")]
    [InlineData("  ma  ")]
    public async Task Search_NeedsAtLeastThreeCharacters(string? fts)
    {
        var (_, service) = await NewAsync();

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.SearchAsync(new RecipientSearchObject { Fts = fts }));

        Assert.Equal([RecipientService.QueryTooShort], exception.Errors["fts"]);
    }

    [Fact]
    public async Task Search_OffersOnlyOtherUsersWhoCanReceiveMoney()
    {
        var (context, service) = await NewAsync();
        await AddWithCardAsync(context, "marko.aktivan");
        await AddWithCardAsync(context, "marko.blokiran", isActive: false);
        await AddWithCardAsync(context, "marko.admin", AppRoles.Admin);
        await AddWithCardAsync(context, "marko.nepotvrdjen", emailConfirmed: false);
        await AddWithCardAsync(context, "marko.bezkartice", cardStatus: CardStatusCodes.Blocked);

        var page = await SearchAsync(service, "marko");

        Assert.Equal("marko.aktivan", Assert.Single(page.Items).Username);
    }

    [Fact]
    public async Task Search_MatchesNameButEmailOnlyExactly()
    {
        var (context, service) = await NewAsync();
        await AddWithCardAsync(context, "ana.kapic");

        Assert.Single((await SearchAsync(service, "Test ana.kapic")).Items);
        Assert.Single((await SearchAsync(service, "ana.kapic@torvin.local")).Items);
        Assert.Empty((await SearchAsync(service, "@torvin.local")).Items);
    }

    [Fact]
    public async Task Search_ReturnsAtMostTenResults()
    {
        var (context, service) = await NewAsync();
        for (var i = 0; i < 12; i++)
        {
            await AddWithCardAsync(context, $"korisnik{i:D2}");
        }

        var page = await SearchAsync(service, "korisnik", pageSize: 50);

        Assert.Equal(RecipientService.MaxResults, page.Items.Count);
        Assert.Equal(12, page.TotalCount);
    }
}
