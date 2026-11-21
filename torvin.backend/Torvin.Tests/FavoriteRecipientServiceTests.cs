using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Torvin.Model.Constants;
using Torvin.Model.Exceptions;
using Torvin.Model.Requests;
using Torvin.Model.Responses;
using Torvin.Model.SearchObjects;
using Torvin.Services.Database;
using Torvin.Services.Database.Entities;
using Torvin.Services.Security;
using Torvin.Services.Services;

namespace Torvin.Tests;

public class FavoriteRecipientServiceTests
{
    private sealed class TestCurrentUser(int userId) : ICurrentUserService
    {
        public int UserId { get; } = userId;
        public string Role => AppRoles.User;
        public string TokenId => "test";
        public DateTime TokenExpiresAt => DateTime.UtcNow.AddHours(1);
    }

    private sealed record Setup(TorvinDbContext Context, User Me, FavoriteRecipientService Service)
    {
        public FavoriteRecipientService As(User user) =>
            new(Context, new TestCurrentUser(user.Id), NullLogger<FavoriteRecipientService>.Instance);
    }

    private static async Task<Setup> NewAsync()
    {
        var context = new TorvinDbContext(TestDatabase.CreateOptions());
        await context.Database.EnsureCreatedAsync();
        var me = await AddWithCardAsync(context, "marko.ja");
        return new Setup(context, me,
            new FavoriteRecipientService(context, new TestCurrentUser(me.Id), NullLogger<FavoriteRecipientService>.Instance));
    }

    private static async Task<User> AddWithCardAsync(TorvinDbContext context, string username, string role = AppRoles.User,
        bool emailConfirmed = true, string cardStatus = CardStatusCodes.Active)
    {
        var user = await TestUsers.AddAsync(context, username, role, emailConfirmed: emailConfirmed);
        await TestCards.AddAsync(context, user, 0m, cardStatus);
        return user;
    }

    private static Task<FavoriteRecipientResponse> AddAsync(FavoriteRecipientService service, User recipient,
        string? nickname = null) =>
        service.CreateAsync(new FavoriteRecipientInsertRequest { RecipientUserId = recipient.Id, Nickname = nickname });

    private static async Task<string> RecipientErrorAsync(Func<Task> action)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(action);
        return Assert.Single(exception.Errors["recipientUserId"]);
    }

    [Fact]
    public async Task Create_SavesTrimmedNickname()
    {
        var setup = await NewAsync();
        var amina = await AddWithCardAsync(setup.Context, "amina.h");

        var favorite = await AddAsync(setup.Service, amina, "  Cimerka  ");

        Assert.Equal(amina.Id, favorite.RecipientUserId);
        Assert.Equal("amina.h", favorite.Username);
        Assert.Equal("Cimerka", favorite.Nickname);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_EmptyNickname_IsStoredAsNull(string? nickname)
    {
        var setup = await NewAsync();
        var amina = await AddWithCardAsync(setup.Context, "amina.h");

        Assert.Null((await AddAsync(setup.Service, amina, nickname)).Nickname);
    }

    [Fact]
    public async Task Create_SameRecipientTwice_IsRejected()
    {
        var setup = await NewAsync();
        var amina = await AddWithCardAsync(setup.Context, "amina.h");
        await AddAsync(setup.Service, amina);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => AddAsync(setup.Service, amina, "Opet"));

        Assert.Equal(FavoriteRecipientService.AlreadyFavorite, exception.Message);
        Assert.Equal(1, await setup.Context.FavoriteRecipients.CountAsync());
    }

    [Fact]
    public async Task Create_Self_IsRejected()
    {
        var setup = await NewAsync();

        Assert.Equal(TransactionService.CannotSendToSelf, await RecipientErrorAsync(() => AddAsync(setup.Service, setup.Me)));
    }

    [Fact]
    public async Task Create_RecipientWhoCannotReceiveMoney_IsRejected()
    {
        var setup = await NewAsync();
        var admin = await AddWithCardAsync(setup.Context, "admin.drugi", AppRoles.Admin);
        var unconfirmed = await AddWithCardAsync(setup.Context, "nepotvrden", emailConfirmed: false);
        var withoutActiveCard = await AddWithCardAsync(setup.Context, "blokirana.kartica", cardStatus: CardStatusCodes.Blocked);

        foreach (var recipient in new[] { admin, unconfirmed, withoutActiveCard })
        {
            Assert.Equal(TransactionService.RecipientNotFound, await RecipientErrorAsync(() => AddAsync(setup.Service, recipient)));
        }

        Assert.Equal(TransactionService.RecipientNotFound, await RecipientErrorAsync(() =>
            setup.Service.CreateAsync(new FavoriteRecipientInsertRequest { RecipientUserId = 999_999 })));
    }

    [Fact]
    public async Task GetMine_ReturnsOnlyOwnFavorites_SearchesNicknameAndName()
    {
        var setup = await NewAsync();
        var amina = await AddWithCardAsync(setup.Context, "amina.h");
        var tarik = await AddWithCardAsync(setup.Context, "tarik.k");
        await AddAsync(setup.Service, amina, "Cimerka");
        await AddAsync(setup.Service, tarik);
        await AddAsync(setup.As(tarik), amina, "Tuđi nadimak");

        var all = await setup.Service.GetMineAsync(new FavoriteRecipientSearchObject());
        var byNickname = await setup.Service.GetMineAsync(new FavoriteRecipientSearchObject { Fts = "Cimer" });
        var byUsername = await setup.Service.GetMineAsync(new FavoriteRecipientSearchObject { Fts = "tarik" });
        var page = await setup.Service.GetMineAsync(new FavoriteRecipientSearchObject { PageSize = 1, Page = 2 });

        Assert.Equal(2, all.TotalCount);
        Assert.DoesNotContain(all.Items, f => f.Nickname == "Tuđi nadimak");
        Assert.Equal("amina.h", Assert.Single(byNickname.Items).Username);
        Assert.Equal("tarik.k", Assert.Single(byUsername.Items).Username);
        Assert.Equal("tarik.k", Assert.Single(page.Items).Username);
    }

    [Fact]
    public async Task Update_ChangesNickname()
    {
        var setup = await NewAsync();
        var amina = await AddWithCardAsync(setup.Context, "amina.h");
        var favorite = await AddAsync(setup.Service, amina, "Amina");

        var updated = await setup.Service.UpdateAsync(favorite.Id, new FavoriteRecipientUpdateRequest { Nickname = " Sestra " });

        Assert.Equal("Sestra", updated.Nickname);
    }

    [Fact]
    public async Task UpdateAndDelete_ForeignFavorite_AreNotFound()
    {
        var setup = await NewAsync();
        var amina = await AddWithCardAsync(setup.Context, "amina.h");
        var tarik = await AddWithCardAsync(setup.Context, "tarik.k");
        var foreign = await AddAsync(setup.As(tarik), amina);

        var update = await Assert.ThrowsAsync<NotFoundException>(
            () => setup.Service.UpdateAsync(foreign.Id, new FavoriteRecipientUpdateRequest { Nickname = "X" }));
        var delete = await Assert.ThrowsAsync<NotFoundException>(() => setup.Service.DeleteAsync(foreign.Id));

        Assert.Equal(FavoriteRecipientService.NotFound, update.Message);
        Assert.Equal(FavoriteRecipientService.NotFound, delete.Message);
        Assert.True(await setup.Context.FavoriteRecipients.AnyAsync(f => f.Id == foreign.Id));
    }

    [Fact]
    public async Task Delete_IsSoft_AndRecipientCanBeAddedAgain()
    {
        var setup = await NewAsync();
        var amina = await AddWithCardAsync(setup.Context, "amina.h");
        var favorite = await AddAsync(setup.Service, amina, "Stari");

        await setup.Service.DeleteAsync(favorite.Id);
        var readded = await AddAsync(setup.Service, amina, "Novi");

        Assert.Equal("Novi", Assert.Single((await setup.Service.GetMineAsync(new FavoriteRecipientSearchObject())).Items).Nickname);
        Assert.NotEqual(favorite.Id, readded.Id);
        Assert.True(await setup.Context.FavoriteRecipients.IgnoreQueryFilters().AnyAsync(f => f.Id == favorite.Id && f.IsDeleted));
        await Assert.ThrowsAsync<NotFoundException>(() => setup.Service.DeleteAsync(favorite.Id));
    }
}
