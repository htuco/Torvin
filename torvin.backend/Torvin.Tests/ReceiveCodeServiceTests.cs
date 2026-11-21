using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Torvin.Model.Constants;
using Torvin.Model.Enums;
using Torvin.Model.Exceptions;
using Torvin.Model.Requests;
using Torvin.Model.Validation;
using Torvin.Services.Database;
using Torvin.Services.Database.Entities;
using Torvin.Services.Security;
using Torvin.Services.Services;

namespace Torvin.Tests;

public class ReceiveCodeServiceTests
{
    private sealed class TestCurrentUser(int userId) : ICurrentUserService
    {
        public int UserId { get; } = userId;
        public string Role => AppRoles.User;
        public string TokenId => "test";
        public DateTime TokenExpiresAt => DateTime.UtcNow.AddHours(1);
    }

    private sealed record Setup(TorvinDbContext Context, User Owner, Card OwnerCard, User Sender)
    {
        public ReceiveCodeService As(User user) => new(Context, new TestCurrentUser(user.Id), ReceiveCodeSignerTests.NewSigner(),
            NullLogger<ReceiveCodeService>.Instance);
    }

    private static async Task<Setup> NewAsync()
    {
        var context = new TorvinDbContext(TestDatabase.CreateOptions());
        await context.Database.EnsureCreatedAsync();
        var owner = await TestUsers.AddAsync(context, "vlasnik.koda", AppRoles.User);
        var card = await TestCards.AddAsync(context, owner, 0m, currencyCode: CurrencyCodes.EUR, last4: "1234");
        var sender = await TestUsers.AddAsync(context, "posiljalac", AppRoles.User);
        await TestCards.AddAsync(context, sender, 100m);
        return new Setup(context, owner, card, sender);
    }

    private static async Task<string> ErrorOfAsync(Func<Task> action)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(action);
        return Assert.Single(exception.Errors["code"]);
    }

    private static async Task SetCardStatusAsync(TorvinDbContext context, Card card, string statusCode)
    {
        card.CardStatusId = await context.CardStatuses.Where(s => s.Code == statusCode).Select(s => s.Id).SingleAsync();
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Create_OwnActiveCard_ReturnsCodeValidForFifteenMinutes()
    {
        var setup = await NewAsync();
        var before = DateTime.UtcNow;

        var response = await setup.As(setup.Owner).CreateAsync(setup.OwnerCard.Id, 25.50m);

        Assert.StartsWith(ReceiveCodeSigner.Prefix, response.Code);
        Assert.Equal(25.50m, response.Amount);
        Assert.InRange(response.ExpiresAt, before.Add(ReceiveCodeService.Lifetime), DateTime.UtcNow.Add(ReceiveCodeService.Lifetime));
    }

    [Fact]
    public async Task Create_ForeignCard_IsNotFound()
    {
        var setup = await NewAsync();

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => setup.As(setup.Sender).CreateAsync(setup.OwnerCard.Id, null));

        Assert.Equal(CardService.NotFound, exception.Message);
    }

    [Theory]
    [InlineData(CardStatusCodes.Blocked)]
    [InlineData(CardStatusCodes.Pending)]
    [InlineData(CardStatusCodes.Closed)]
    public async Task Create_CardNotActive_IsRejected(string statusCode)
    {
        var setup = await NewAsync();
        await SetCardStatusAsync(setup.Context, setup.OwnerCard, statusCode);

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => setup.As(setup.Owner).CreateAsync(setup.OwnerCard.Id, null));

        Assert.Equal(ReceiveCodeService.CardNotActive, exception.Message);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0.001")]
    [InlineData("100000.01")]
    public async Task Create_InvalidAmount_IsRejected(string amount)
    {
        var setup = await NewAsync();

        var exception = await Assert.ThrowsAsync<ValidationException>(() => setup.As(setup.Owner)
            .CreateAsync(setup.OwnerCard.Id, decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));

        Assert.Equal([Money.InvalidAmount], exception.Errors["amount"]);
    }

    [Fact]
    public async Task Resolve_ValidCode_ReturnsRecipientWithMaskedCard()
    {
        var setup = await NewAsync();
        var code = (await setup.As(setup.Owner).CreateAsync(setup.OwnerCard.Id, 25.50m)).Code;

        var recipient = await setup.As(setup.Sender).ResolveAsync(new ReceiveCodeResolveRequest { Code = $"  {code} " });

        Assert.Equal(setup.Owner.Id, recipient.UserId);
        Assert.Equal("vlasnik.koda", recipient.Username);
        Assert.Equal(CardNetwork.Visa, recipient.Network);
        Assert.Equal("1234", recipient.Last4Digits);
        Assert.Equal(CurrencyCodes.EUR, recipient.CurrencyCode);
        Assert.Equal(25.50m, recipient.Amount);
    }

    [Fact]
    public async Task Resolve_ExpiredCode_IsRejected()
    {
        var setup = await NewAsync();
        var code = ReceiveCodeSignerTests.NewSigner()
            .Create(new ReceiveCodePayload(setup.OwnerCard.Id, null, DateTime.UtcNow.AddSeconds(-1)));

        var error = await ErrorOfAsync(() => setup.As(setup.Sender).ResolveAsync(new ReceiveCodeResolveRequest { Code = code }));

        Assert.Equal(ReceiveCodeService.CodeExpired, error);
    }

    [Fact]
    public async Task Resolve_CodeSignedWithOtherKey_IsInvalid()
    {
        var setup = await NewAsync();
        var code = ReceiveCodeSignerTests.NewSigner("0123456789abcdef0123456789abcdef")
            .Create(new ReceiveCodePayload(setup.OwnerCard.Id, null, DateTime.UtcNow.AddMinutes(5)));

        var error = await ErrorOfAsync(() => setup.As(setup.Sender).ResolveAsync(new ReceiveCodeResolveRequest { Code = code }));

        Assert.Equal(ReceiveCodeService.CodeInvalid, error);
    }

    [Fact]
    public async Task Resolve_OwnCode_IsRejected()
    {
        var setup = await NewAsync();
        var code = (await setup.As(setup.Owner).CreateAsync(setup.OwnerCard.Id, null)).Code;

        var error = await ErrorOfAsync(() => setup.As(setup.Owner).ResolveAsync(new ReceiveCodeResolveRequest { Code = code }));

        Assert.Equal(TransactionService.CannotSendToSelf, error);
    }

    [Fact]
    public async Task Resolve_CardBlockedAfterCodeWasIssued_IsRejected()
    {
        var setup = await NewAsync();
        var code = (await setup.As(setup.Owner).CreateAsync(setup.OwnerCard.Id, null)).Code;
        await SetCardStatusAsync(setup.Context, setup.OwnerCard, CardStatusCodes.Blocked);

        var error = await ErrorOfAsync(() => setup.As(setup.Sender).ResolveAsync(new ReceiveCodeResolveRequest { Code = code }));

        Assert.Equal(TransactionService.RecipientNotFound, error);
    }

    [Fact]
    public async Task Resolve_OwnerDeactivatedAfterCodeWasIssued_IsRejected()
    {
        var setup = await NewAsync();
        var code = (await setup.As(setup.Owner).CreateAsync(setup.OwnerCard.Id, null)).Code;
        setup.Owner.IsActive = false;
        await setup.Context.SaveChangesAsync();

        var error = await ErrorOfAsync(() => setup.As(setup.Sender).ResolveAsync(new ReceiveCodeResolveRequest { Code = code }));

        Assert.Equal(TransactionService.RecipientNotFound, error);
    }
}
