using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Torvin.Model.Constants;
using Torvin.Model.Enums;
using Torvin.Model.Exceptions;
using Torvin.Model.Requests;
using Torvin.Services.Configuration;
using Torvin.Services.Database;
using Torvin.Services.Security;
using Torvin.Services.Services;

namespace Torvin.Tests;

public class AuthServiceTests
{
    public static readonly JwtOptions Jwt = new()
    {
        Key = "0123456789abcdef0123456789abcdef", Issuer = "torvin-test", Audience = "torvin-test", ExpiresMinutes = 60,
    };

    // Odjava se ovdje ne testira, pa trenutni korisnik nije potreban
    private sealed class NoCurrentUser : ICurrentUserService
    {
        public int UserId => throw new InvalidOperationException();
        public string Role => throw new InvalidOperationException();
        public string TokenId => throw new InvalidOperationException();
        public DateTime TokenExpiresAt => throw new InvalidOperationException();
    }

    private static async Task<TorvinDbContext> NewContextAsync()
    {
        var context = new TorvinDbContext(TestDatabase.CreateOptions());
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static AuthService NewService(TorvinDbContext context) => new(
        context,
        new JwtTokenService(Options.Create(Jwt)),
        new TokenRevocationService(context, new MemoryCache(new MemoryCacheOptions())),
        new NoCurrentUser(),
        NullLogger<AuthService>.Instance);

    private static LoginRequest Login(string login, string password = TestUsers.Password, ClientApp app = ClientApp.Mobile) =>
        new() { Login = login, Password = password, App = app };

    [Fact]
    public async Task CorrectPassword_ReturnsTokenAndResetsFailedCount()
    {
        await using var context = await NewContextAsync();
        var user = await TestUsers.AddAsync(context, "amina", AppRoles.User);
        user.FailedLoginCount = 3;
        await context.SaveChangesAsync();

        var response = await NewService(context).LoginAsync(Login("amina"));

        Assert.False(string.IsNullOrEmpty(response.Token));
        Assert.Equal(AppRoles.User, response.Role);
        Assert.Equal(0, user.FailedLoginCount);
    }

    [Fact]
    public async Task LoginByEmail_IgnoresLetterCase()
    {
        await using var context = await NewContextAsync();
        await TestUsers.AddAsync(context, "amina", AppRoles.User);

        var response = await NewService(context).LoginAsync(Login(" Amina@Torvin.Local "));

        Assert.Equal(AppRoles.User, response.Role);
    }

    [Fact]
    public async Task UnknownUserAndWrongPassword_HaveSameMessage()
    {
        await using var context = await NewContextAsync();
        await TestUsers.AddAsync(context, "amina", AppRoles.User);
        var service = NewService(context);

        var unknown = await Assert.ThrowsAsync<BusinessException>(() => service.LoginAsync(Login("nepostojeci")));
        var wrong = await Assert.ThrowsAsync<BusinessException>(() => service.LoginAsync(Login("amina", "Pogresna.1")));

        Assert.Equal(AuthService.InvalidCredentials, unknown.Message);
        Assert.Equal(unknown.Message, wrong.Message);
    }

    [Fact]
    public async Task FiveWrongPasswords_LockAccountEvenForCorrectPassword()
    {
        await using var context = await NewContextAsync();
        var user = await TestUsers.AddAsync(context, "amina", AppRoles.User);
        var service = NewService(context);

        for (var i = 0; i < AuthService.MaxFailedLogins; i++)
        {
            await Assert.ThrowsAsync<BusinessException>(() => service.LoginAsync(Login("amina", "Pogresna.1")));
        }

        var locked = await Assert.ThrowsAsync<ForbiddenException>(() => service.LoginAsync(Login("amina")));
        Assert.Contains("zaključan", locked.Message);
        Assert.True(user.LockoutEndAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task ExpiredLockout_AllowsLogin()
    {
        await using var context = await NewContextAsync();
        var user = await TestUsers.AddAsync(context, "amina", AppRoles.User);
        user.LockoutEndAt = DateTime.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();

        await NewService(context).LoginAsync(Login("amina"));

        Assert.Null(user.LockoutEndAt);
    }

    [Fact]
    public async Task BlockedAccount_IsRejected()
    {
        await using var context = await NewContextAsync();
        await TestUsers.AddAsync(context, "amina", AppRoles.User, isActive: false);

        var exception = await Assert.ThrowsAsync<ForbiddenException>(() => NewService(context).LoginAsync(Login("amina")));

        Assert.Contains("blokiran", exception.Message);
    }

    [Fact]
    public async Task UnconfirmedEmail_IsRejected()
    {
        await using var context = await NewContextAsync();
        await TestUsers.AddAsync(context, "amina", AppRoles.User, emailConfirmed: false);

        var exception = await Assert.ThrowsAsync<ForbiddenException>(() => NewService(context).LoginAsync(Login("amina")));

        Assert.Contains("Email nije potvrđen", exception.Message);
    }

    [Theory]
    [InlineData(AppRoles.User, ClientApp.Desktop)]
    [InlineData(AppRoles.Admin, ClientApp.Mobile)]
    public async Task WrongApplicationForRole_IsRejected(string role, ClientApp app)
    {
        await using var context = await NewContextAsync();
        await TestUsers.AddAsync(context, "korisnik", role);

        await Assert.ThrowsAsync<ForbiddenException>(() => NewService(context).LoginAsync(Login("korisnik", app: app)));
    }

    [Fact]
    public async Task RevokedToken_IsReportedRevokedFromDatabase()
    {
        await using var context = await NewContextAsync();
        var user = await TestUsers.AddAsync(context, "amina", AppRoles.User);
        await new TokenRevocationService(context, new MemoryCache(new MemoryCacheOptions()))
            .RevokeAsync("jti-1", user.Id, DateTime.UtcNow.AddMinutes(30));

        // Novi keš, da odgovor dođe iz baze, a ne iz keša koji je postavila odjava
        var fresh = new TokenRevocationService(context, new MemoryCache(new MemoryCacheOptions()));

        Assert.True(await fresh.IsRevokedAsync("jti-1"));
        Assert.False(await fresh.IsRevokedAsync("jti-2"));
        Assert.Single(await context.RevokedTokens.ToListAsync());
    }

    [Fact]
    public async Task LockedAccountWithWrongPassword_LooksLikeUnknownAccount()
    {
        await using var context = await NewContextAsync();
        var user = await TestUsers.AddAsync(context, "amina", AppRoles.User);
        user.LockoutEndAt = DateTime.UtcNow.AddMinutes(10);
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(() => NewService(context).LoginAsync(Login("amina", "Pogresna.1")));

        Assert.Equal(AuthService.InvalidCredentials, exception.Message);
    }

    [Fact]
    public async Task FailedLoginCount_IsConcurrencyToken()
    {
        // Dva istovremena pogrešna pokušaja čitaju isti brojač. Drugi upis mora pasti, da ga servis pročita ponovo
        var options = TestDatabase.CreateOptions();
        await using (var setup = new TorvinDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            await TestUsers.AddAsync(setup, "amina", AppRoles.User);
        }

        await using var first = new TorvinDbContext(options);
        await using var second = new TorvinDbContext(options);
        var userInFirst = await first.Users.SingleAsync(u => u.Username == "amina");
        var userInSecond = await second.Users.SingleAsync(u => u.Username == "amina");

        userInFirst.FailedLoginCount++;
        await first.SaveChangesAsync();
        userInSecond.FailedLoginCount++;

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task RevokeTwice_KeepsOneRecord()
    {
        var options = TestDatabase.CreateOptions();
        int userId;
        await using (var setup = new TorvinDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            userId = (await TestUsers.AddAsync(setup, "amina", AppRoles.User)).Id;
        }

        foreach (var _ in Enumerable.Range(0, 2))
        {
            await using var context = new TorvinDbContext(options);
            await new TokenRevocationService(context, new MemoryCache(new MemoryCacheOptions()))
                .RevokeAsync("jti-1", userId, DateTime.UtcNow.AddMinutes(30));
        }

        await using var check = new TorvinDbContext(options);
        Assert.Single(await check.RevokedTokens.ToListAsync());
    }

    [Fact]
    public async Task RevokedMark_IsNotOverwrittenByEarlierCheck()
    {
        await using var context = await NewContextAsync();
        var user = await TestUsers.AddAsync(context, "amina", AppRoles.User);
        var service = new TokenRevocationService(context, new MemoryCache(new MemoryCacheOptions()));

        Assert.False(await service.IsRevokedAsync("jti-1"));
        await service.RevokeAsync("jti-1", user.Id, DateTime.UtcNow.AddMinutes(30));

        Assert.True(await service.IsRevokedAsync("jti-1"));
    }
}
