using Microsoft.EntityFrameworkCore;
using Torvin.Model.Constants;
using Torvin.Model.Enums;
using Torvin.Services.Database;
using Torvin.Services.Database.Entities;
using Torvin.Services.Security;

namespace Torvin.Tests;

public class EmailCodeServiceTests
{
    private const EmailTokenPurpose Purpose = EmailTokenPurpose.Registration;

    private static async Task<(TorvinDbContext Context, User User)> NewUserAsync(DbContextOptions<TorvinDbContext>? options = null)
    {
        var context = new TorvinDbContext(options ?? TestDatabase.CreateOptions());
        await context.Database.EnsureCreatedAsync();
        var user = await TestUsers.AddAsync(context, "amina", AppRoles.User, emailConfirmed: false);
        return (context, user);
    }

    private static async Task<string> CreateCodeAsync(TorvinDbContext context, User user)
    {
        var code = await new EmailCodeService(context).CreateAsync(user, Purpose);
        await context.SaveChangesAsync();
        return code;
    }

    private static async Task<Card> AddPendingCardAsync(TorvinDbContext context, User user, string last4)
    {
        var card = new Card
        {
            User = user, CardTypeId = 1, CurrencyId = 1, CardStatusId = 1, Network = CardNetwork.Visa,
            Last4Digits = last4, ExpiresAt = new DateOnly(2030, 12, 31), RowVersion = [1],
        };
        context.Cards.Add(card);
        await context.SaveChangesAsync();
        return card;
    }

    private static async Task<string> CreateCardCodeAsync(TorvinDbContext context, Card card)
    {
        var code = await new EmailCodeService(context).CreateForCardAsync(card);
        await context.SaveChangesAsync();
        return code;
    }

    // Novi kod kartice poništava samo stari kod iste kartice, a kod druge kartice ostaje važeći
    [Fact]
    public async Task CardCode_IsBoundToItsCard()
    {
        var (context, user) = await NewUserAsync();
        await using var _ = context;
        var first = await AddPendingCardAsync(context, user, "1111");
        var second = await AddPendingCardAsync(context, user, "2222");
        var firstOld = await CreateCardCodeAsync(context, first);
        var firstNew = await CreateCardCodeAsync(context, first);
        var secondCode = await CreateCardCodeAsync(context, second);
        var service = new EmailCodeService(context);

        if (firstOld != firstNew)
        {
            Assert.Null(await service.FindValidForCardAsync(first.Id, firstOld));
        }

        if (secondCode != firstNew)
        {
            Assert.Null(await service.FindValidForCardAsync(second.Id, firstNew));
        }

        Assert.NotNull(await service.FindValidForCardAsync(first.Id, firstNew));
        Assert.NotNull(await service.FindValidForCardAsync(second.Id, secondCode));
    }

    [Fact]
    public async Task CardCode_ExpiresAfter15Minutes()
    {
        var (context, user) = await NewUserAsync();
        await using var _ = context;
        var card = await AddPendingCardAsync(context, user, "1111");
        await CreateCardCodeAsync(context, card);

        var token = await context.EmailTokens.SingleAsync();

        var lifetime = token.ExpiresAt - token.CreatedAt;
        Assert.InRange(lifetime, EmailCodeService.CardActivationLifetime - TimeSpan.FromSeconds(5),
            EmailCodeService.CardActivationLifetime + TimeSpan.FromSeconds(5));
        Assert.Equal((card.Id, EmailTokenPurpose.CardActivation), (token.CardId!.Value, token.Purpose));
    }

    [Fact]
    public async Task CorrectCode_IsFound_AndStoredOnlyAsHash()
    {
        var (context, user) = await NewUserAsync();
        await using var _ = context;

        var code = await CreateCodeAsync(context, user);
        var token = await new EmailCodeService(context).FindValidAsync(user.Email, code, Purpose);

        Assert.NotNull(token);
        Assert.Matches("^[0-9]{6}$", code);
        Assert.StartsWith("PBKDF2-SHA256$", token.CodeHash);
        Assert.DoesNotContain(code, token.CodeHash);
        Assert.Equal(1, token.AttemptCount);
    }

    [Fact]
    public async Task UsedCode_IsNotFoundAgain()
    {
        var (context, user) = await NewUserAsync();
        await using var _ = context;
        var code = await CreateCodeAsync(context, user);
        var service = new EmailCodeService(context);

        var token = await service.FindValidAsync(user.Email, code, Purpose);
        Assert.True(await service.UseAsync(token!));

        Assert.Null(await service.FindValidAsync(user.Email, code, Purpose));
    }

    [Fact]
    public async Task ExpiredCode_IsNotFound()
    {
        var (context, user) = await NewUserAsync();
        await using var _ = context;
        var code = await CreateCodeAsync(context, user);
        var stored = await context.EmailTokens.SingleAsync();
        stored.ExpiresAt = DateTime.UtcNow.AddSeconds(-1);
        await context.SaveChangesAsync();

        Assert.Null(await new EmailCodeService(context).FindValidAsync(user.Email, code, Purpose));
    }

    [Fact]
    public async Task AfterFiveWrongAttempts_CorrectCodeIsRejected()
    {
        var (context, user) = await NewUserAsync();
        await using var _ = context;
        var code = await CreateCodeAsync(context, user);
        var wrong = code == "000000" ? "111111" : "000000";
        var service = new EmailCodeService(context);

        for (var i = 0; i < EmailCodeService.MaxAttempts; i++)
        {
            Assert.Null(await service.FindValidAsync(user.Email, wrong, Purpose));
        }

        Assert.Null(await service.FindValidAsync(user.Email, code, Purpose));
        Assert.Equal(EmailCodeService.MaxAttempts, (await context.EmailTokens.SingleAsync()).AttemptCount);
    }

    [Fact]
    public async Task NewCode_InvalidatesPreviousCodeOfSamePurpose()
    {
        var (context, user) = await NewUserAsync();
        await using var _ = context;
        var first = await CreateCodeAsync(context, user);
        var second = await CreateCodeAsync(context, user);
        var service = new EmailCodeService(context);

        if (first != second)
        {
            Assert.Null(await service.FindValidAsync(user.Email, first, Purpose));
        }

        Assert.NotNull(await service.FindValidAsync(user.Email, second, Purpose));
        Assert.Equal(1, await context.EmailTokens.CountAsync(t => t.ExpiresAt > DateTime.UtcNow));
    }

    [Fact]
    public async Task CodeOfOtherPurpose_IsNotFound()
    {
        var (context, user) = await NewUserAsync();
        await using var _ = context;
        var code = await CreateCodeAsync(context, user);

        Assert.Null(await new EmailCodeService(context).FindValidAsync(user.Email, code, EmailTokenPurpose.PasswordReset));
    }

    [Fact]
    public async Task CanSend_IsFalseWithinResendInterval()
    {
        var (context, user) = await NewUserAsync();
        await using var _ = context;
        var service = new EmailCodeService(context);

        Assert.True(await service.CanSendAsync(user.Id, Purpose));
        await CreateCodeAsync(context, user);

        Assert.False(await service.CanSendAsync(user.Id, Purpose));
        Assert.True(await service.CanSendAsync(user.Id, EmailTokenPurpose.PasswordReset));
    }

    [Fact]
    public async Task CodeUsedByAnotherRequest_CannotBeUsedAgain()
    {
        var options = TestDatabase.CreateOptions();
        var (first, user) = await NewUserAsync(options);
        await using var _ = first;
        var code = await CreateCodeAsync(first, user);
        await using var second = new TorvinDbContext(options);

        // Oba zahtjeva su pročitala kod prije nego što ga je prvi iskoristio
        var firstToken = await new EmailCodeService(first).FindValidAsync(user.Email, code, Purpose);
        var secondToken = await new EmailCodeService(second).FindValidAsync(user.Email, code, Purpose);
        Assert.NotNull(firstToken);
        Assert.NotNull(secondToken);

        Assert.True(await new EmailCodeService(first).UseAsync(firstToken));
        Assert.False(await new EmailCodeService(second).UseAsync(secondToken));
    }

    [Fact]
    public async Task WrongAttemptFromAnotherRequest_DoesNotRejectCorrectCode()
    {
        var options = TestDatabase.CreateOptions();
        var (first, user) = await NewUserAsync(options);
        await using var _ = first;
        var code = await CreateCodeAsync(first, user);
        var wrong = code == "000000" ? "111111" : "000000";
        await using var second = new TorvinDbContext(options);

        var token = await new EmailCodeService(first).FindValidAsync(user.Email, code, Purpose);
        Assert.Null(await new EmailCodeService(second).FindValidAsync(user.Email, wrong, Purpose));

        // Pogrešan pokušaj drugog zahtjeva je povećao brojač, ali kod nije iskorišten
        user.EmailConfirmed = true;
        Assert.True(await new EmailCodeService(first).UseAsync(token!));

        await using var check = new TorvinDbContext(options);
        Assert.True((await check.Users.SingleAsync(u => u.Id == user.Id)).EmailConfirmed);
        Assert.NotNull((await check.EmailTokens.SingleAsync()).UsedAt);
    }
}
