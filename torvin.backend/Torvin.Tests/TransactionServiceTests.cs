using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Torvin.Model.Constants;
using Torvin.Model.Enums;
using Torvin.Model.Exceptions;
using Torvin.Model.Messages;
using Torvin.Model.Requests;
using Torvin.Model.SearchObjects;
using Torvin.Model.Validation;
using Torvin.Services.Database;
using Torvin.Services.Database.Entities;
using Torvin.Services.Security;
using Torvin.Services.Services;

namespace Torvin.Tests;

// Pravila prenosa i historije nad bazom u memoriji. Atomičnost i istovremenost su u TransactionPostingSqlServerTests
public class TransactionServiceTests
{
    private sealed class TestCurrentUser : ICurrentUserService
    {
        public int UserId { get; set; }
        public string Role => AppRoles.User;
        public string TokenId => "test";
        public DateTime TokenExpiresAt => DateTime.UtcNow.AddHours(1);
    }

    private sealed class Fixture(TorvinDbContext context, User sender, User receiver)
    {
        public TorvinDbContext Context { get; } = context;
        public User Sender { get; } = sender;
        public User Receiver { get; } = receiver;
        public TestCurrentUser CurrentUser { get; } = new() { UserId = sender.Id };
        public FakeMessagePublisher Publisher { get; } = new();

        public TransactionService Service =>
            new(Context, CurrentUser, Publisher, NullLogger<TransactionService>.Instance);

        public void ActAs(User user) => CurrentUser.UserId = user.Id;
    }

    private static async Task<Fixture> NewFixtureAsync()
    {
        var context = new TorvinDbContext(TestDatabase.CreateOptions());
        await context.Database.EnsureCreatedAsync();
        var sender = await TestUsers.AddAsync(context, "posiljalac", AppRoles.User);
        var receiver = await TestUsers.AddAsync(context, "primalac", AppRoles.User);
        return new Fixture(context, sender, receiver);
    }

    private static TransactionInsertRequest Send(Card from, User to, decimal amount, string? note = null) => new()
    {
        FromCardId = from.Id,
        ToUserId = to.Id,
        Amount = amount,
        Note = note,
    };

    private static async Task<ValidationException> FieldErrorAsync(Func<Task> action, string field, string message)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(action);
        Assert.Equal([message], exception.Errors[field]);
        return exception;
    }

    private static async Task<decimal> BalanceAsync(Fixture fixture, Card card) =>
        await fixture.Context.Cards.AsNoTracking().Where(c => c.Id == card.Id).Select(c => c.Balance).SingleAsync();

    [Fact]
    public async Task Create_MovesMoney_RecordsTransactionAndAudit_AndPublishesEvent()
    {
        var fixture = await NewFixtureAsync();
        var from = await TestCards.AddAsync(fixture.Context, fixture.Sender, 100m, last4: "1111");
        var to = await TestCards.AddAsync(fixture.Context, fixture.Receiver, 10m, isDefault: true, last4: "2222");

        var response = await fixture.Service.CreateAsync(Send(from, fixture.Receiver, 40.5m, "  Ručak  "));

        Assert.Equal(59.5m, await BalanceAsync(fixture, from));
        Assert.Equal(50.5m, await BalanceAsync(fixture, to));

        var transaction = await fixture.Context.Transactions.AsNoTracking()
            .Include(t => t.TransactionStatus).Include(t => t.TransactionType).SingleAsync();
        Assert.Equal(TransactionStatusCodes.Completed, transaction.TransactionStatus.Code);
        Assert.Equal(TransactionTypeCodes.Transfer, transaction.TransactionType.Code);
        Assert.Equal(from.CurrencyId, transaction.CurrencyId);
        Assert.Equal("Ručak", transaction.Note);
        Assert.Matches(@"^TRV\d{14}$", transaction.ReferenceNumber);

        Assert.Equal(TransactionDirection.Sent, response.Direction);
        Assert.Equal("1111", response.MyCardLast4Digits);
        Assert.Equal("2222", response.CounterpartyCardLast4Digits);
        Assert.Equal(fixture.Receiver.Id, response.CounterpartyUserId);

        var audit = await fixture.Context.AuditLogs.AsNoTracking().SingleAsync();
        Assert.Equal(AuditActions.TransactionCompleted, audit.Action);
        Assert.Equal(transaction.Id, audit.EntityId);
        Assert.Equal("""{"FromCardBalance":"100.00","ToCardBalance":"10.00"}""", audit.OldValues);
        Assert.Equal("""{"FromCardBalance":"59.50","ToCardBalance":"50.50"}""", audit.NewValues);

        var (exchange, message) = Assert.Single(fixture.Publisher.Published);
        Assert.Equal(ExchangeNames.TransactionCompleted, exchange);
        var completed = Assert.IsType<TransactionCompleted>(message);
        Assert.Equal(transaction.Id, completed.TransactionId);
        Assert.Equal(fixture.Sender.Id, completed.FromUserId);
        Assert.Equal(fixture.Receiver.Id, completed.ToUserId);
        Assert.Equal(40.5m, completed.Amount);
        Assert.Equal(CurrencyCodes.BAM, completed.CurrencyCode);
    }

    [Fact]
    public async Task Create_WithInsufficientFunds_ChangesNothing()
    {
        var fixture = await NewFixtureAsync();
        var from = await TestCards.AddAsync(fixture.Context, fixture.Sender, 20m, last4: "1111");
        await TestCards.AddAsync(fixture.Context, fixture.Receiver, 0m);

        await FieldErrorAsync(() => fixture.Service.CreateAsync(Send(from, fixture.Receiver, 20.01m)),
            "amount", TransactionService.InsufficientFunds("1111"));

        Assert.Equal(20m, await BalanceAsync(fixture, from));
        Assert.False(await fixture.Context.Transactions.AnyAsync());
        Assert.Empty(fixture.Publisher.Published);
    }

    [Theory]
    [InlineData(CardStatusCodes.Blocked)]
    [InlineData(CardStatusCodes.Pending)]
    public async Task Create_FromInactiveCard_IsRejected(string statusCode)
    {
        var fixture = await NewFixtureAsync();
        var from = await TestCards.AddAsync(fixture.Context, fixture.Sender, 100m, statusCode, last4: "1111");
        await TestCards.AddAsync(fixture.Context, fixture.Receiver, 0m);

        await FieldErrorAsync(() => fixture.Service.CreateAsync(Send(from, fixture.Receiver, 10m)),
            "fromCardId", TransactionService.CardNotActive("1111"));
    }

    [Fact]
    public async Task Create_FromSomeoneElsesCard_LooksLikeMissingCard()
    {
        var fixture = await NewFixtureAsync();
        var foreign = await TestCards.AddAsync(fixture.Context, fixture.Receiver, 100m);
        var third = await TestUsers.AddAsync(fixture.Context, "treci", AppRoles.User);
        await TestCards.AddAsync(fixture.Context, third, 0m);

        await FieldErrorAsync(() => fixture.Service.CreateAsync(Send(foreign, third, 10m)),
            "fromCardId", TransactionService.CardNotFound);
    }

    [Fact]
    public async Task Create_ToSelf_IsRejected()
    {
        var fixture = await NewFixtureAsync();
        var from = await TestCards.AddAsync(fixture.Context, fixture.Sender, 100m);
        await TestCards.AddAsync(fixture.Context, fixture.Sender, 0m, last4: "9999");

        await FieldErrorAsync(() => fixture.Service.CreateAsync(Send(from, fixture.Sender, 10m)),
            "toUserId", TransactionService.CannotSendToSelf);
    }

    [Theory]
    [InlineData("blokiran")]
    [InlineData("admin")]
    [InlineData("nepotvrdjen")]
    [InlineData("nepostojeci")]
    public async Task Create_ToUnavailableRecipient_GivesSameMessage(string kind)
    {
        var fixture = await NewFixtureAsync();
        var from = await TestCards.AddAsync(fixture.Context, fixture.Sender, 100m);
        var recipient = kind switch
        {
            "blokiran" => await TestUsers.AddAsync(fixture.Context, kind, AppRoles.User, isActive: false),
            "admin" => await TestUsers.AddAsync(fixture.Context, kind, AppRoles.Admin),
            "nepotvrdjen" => await TestUsers.AddAsync(fixture.Context, kind, AppRoles.User, emailConfirmed: false),
            _ => new User { Id = 9999 },
        };
        if (recipient.Id != 9999)
        {
            await TestCards.AddAsync(fixture.Context, recipient, 0m);
        }

        await FieldErrorAsync(() => fixture.Service.CreateAsync(Send(from, recipient, 10m)),
            "toUserId", TransactionService.RecipientNotFound);
    }

    [Fact]
    public async Task Create_RecipientWithoutCardInSameCurrency_IsRejected()
    {
        var fixture = await NewFixtureAsync();
        var from = await TestCards.AddAsync(fixture.Context, fixture.Sender, 100m);
        await TestCards.AddAsync(fixture.Context, fixture.Receiver, 0m, currencyCode: CurrencyCodes.EUR);

        await FieldErrorAsync(() => fixture.Service.CreateAsync(Send(from, fixture.Receiver, 10m)),
            "toUserId", TransactionService.RecipientHasNoCard(CurrencyCodes.BAM));
    }

    [Fact]
    public async Task Create_WhenDefaultCardIsBlocked_GoesToOldestActiveCardInSameCurrency()
    {
        var fixture = await NewFixtureAsync();
        var from = await TestCards.AddAsync(fixture.Context, fixture.Sender, 100m);
        await TestCards.AddAsync(fixture.Context, fixture.Receiver, 0m, CardStatusCodes.Blocked, isDefault: true, last4: "0001");
        await TestCards.AddAsync(fixture.Context, fixture.Receiver, 0m, currencyCode: CurrencyCodes.EUR, last4: "0002");
        var oldestBam = await TestCards.AddAsync(fixture.Context, fixture.Receiver, 0m, last4: "0003");
        await TestCards.AddAsync(fixture.Context, fixture.Receiver, 0m, last4: "0004");

        var response = await fixture.Service.CreateAsync(Send(from, fixture.Receiver, 10m));

        Assert.Equal("0003", response.CounterpartyCardLast4Digits);
        Assert.Equal(10m, await BalanceAsync(fixture, oldestBam));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("1.005")]
    [InlineData("100000.01")]
    public async Task Create_WithInvalidAmount_IsRejected(string amount)
    {
        var fixture = await NewFixtureAsync();
        var from = await TestCards.AddAsync(fixture.Context, fixture.Sender, 200_000m);
        await TestCards.AddAsync(fixture.Context, fixture.Receiver, 0m);

        await FieldErrorAsync(() => fixture.Service.CreateAsync(
                Send(from, fixture.Receiver, decimal.Parse(amount, CultureInfo.InvariantCulture))),
            "amount", Money.InvalidAmount);
    }

    [Fact]
    public async Task Create_WithMissingCategory_IsRejected()
    {
        var fixture = await NewFixtureAsync();
        var from = await TestCards.AddAsync(fixture.Context, fixture.Sender, 100m);
        await TestCards.AddAsync(fixture.Context, fixture.Receiver, 0m);
        var request = Send(from, fixture.Receiver, 10m);
        request.TransactionCategoryId = 9999;

        await FieldErrorAsync(() => fixture.Service.CreateAsync(request),
            "transactionCategoryId", TransactionService.CategoryMissing);
    }

    [Fact]
    public async Task Create_ByBlockedSender_IsForbidden()
    {
        var fixture = await NewFixtureAsync();
        var from = await TestCards.AddAsync(fixture.Context, fixture.Sender, 100m);
        await TestCards.AddAsync(fixture.Context, fixture.Receiver, 0m);
        fixture.Sender.IsActive = false;
        await fixture.Context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ForbiddenException>(
            () => fixture.Service.CreateAsync(Send(from, fixture.Receiver, 10m)));
        Assert.Equal(AuthService.AccountBlocked, exception.Message);
    }

    // Tri prenosa: posiljalac → primalac, primalac → posiljalac, treći → primalac (posiljalac ga ne smije vidjeti)
    private static async Task<(Fixture Fixture, Card SenderCard, Card ReceiverCard)> HistoryAsync()
    {
        var fixture = await NewFixtureAsync();
        var senderCard = await TestCards.AddAsync(fixture.Context, fixture.Sender, 500m, isDefault: true, last4: "1111");
        var receiverCard = await TestCards.AddAsync(fixture.Context, fixture.Receiver, 500m, isDefault: true, last4: "2222");
        var third = await TestUsers.AddAsync(fixture.Context, "treci", AppRoles.User);
        var thirdCard = await TestCards.AddAsync(fixture.Context, third, 500m, last4: "3333");

        await fixture.Service.CreateAsync(Send(senderCard, fixture.Receiver, 10m, "Kafa"));
        fixture.ActAs(fixture.Receiver);
        await fixture.Service.CreateAsync(Send(receiverCard, fixture.Sender, 25m, "Kino"));
        fixture.ActAs(third);
        await fixture.Service.CreateAsync(Send(thirdCard, fixture.Receiver, 99m));
        fixture.ActAs(fixture.Sender);
        return (fixture, senderCard, receiverCard);
    }

    [Fact]
    public async Task GetMine_ShowsSentAndReceived_NewestFirst_AndNotOthers()
    {
        var (fixture, _, _) = await HistoryAsync();

        var page = await fixture.Service.GetMineAsync(new TransactionSearchObject());

        Assert.Equal(2, page.TotalCount);
        Assert.Equal([25m, 10m], page.Items.Select(t => t.Amount));
        Assert.Equal([TransactionDirection.Received, TransactionDirection.Sent], page.Items.Select(t => t.Direction));
        Assert.All(page.Items, t => Assert.Equal("1111", t.MyCardLast4Digits));
        Assert.All(page.Items, t => Assert.Equal("2222", t.CounterpartyCardLast4Digits));
    }

    [Fact]
    public async Task GetMine_HidesFailedTransfersFromReceiver()
    {
        var (fixture, _, _) = await HistoryAsync();
        var failed = await fixture.Context.TransactionStatuses.SingleAsync(s => s.Code == TransactionStatusCodes.Failed);
        var sent = await fixture.Context.Transactions.SingleAsync(t => t.SenderUserId == fixture.Sender.Id);
        sent.TransactionStatusId = failed.Id;
        await fixture.Context.SaveChangesAsync();

        Assert.Equal(2, (await fixture.Service.GetMineAsync(new TransactionSearchObject())).TotalCount);
        fixture.ActAs(fixture.Receiver);
        var receiverPage = await fixture.Service.GetMineAsync(new TransactionSearchObject());

        Assert.DoesNotContain(receiverPage.Items, t => t.Id == sent.Id);
    }

    [Fact]
    public async Task GetMine_FiltersByDirectionAmountAndText()
    {
        var (fixture, _, _) = await HistoryAsync();

        var sent = await fixture.Service.GetMineAsync(new TransactionSearchObject { Direction = TransactionDirection.Sent });
        var bigger = await fixture.Service.GetMineAsync(new TransactionSearchObject { AmountFrom = 20m, AmountTo = 30m });
        var byNote = await fixture.Service.GetMineAsync(new TransactionSearchObject { Fts = "Kafa" });
        var byCounterparty = await fixture.Service.GetMineAsync(new TransactionSearchObject { Fts = "primalac" });
        var byThirdParty = await fixture.Service.GetMineAsync(new TransactionSearchObject { Fts = "treci" });

        Assert.Equal(10m, Assert.Single(sent.Items).Amount);
        Assert.Equal(25m, Assert.Single(bigger.Items).Amount);
        Assert.Equal(10m, Assert.Single(byNote.Items).Amount);
        Assert.Equal(2, byCounterparty.TotalCount);
        Assert.Equal(0, byThirdParty.TotalCount);
    }

    [Fact]
    public async Task GetMine_FiltersByDate_Inclusive()
    {
        var (fixture, _, _) = await HistoryAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var todayOnly = await fixture.Service.GetMineAsync(new TransactionSearchObject { DateFrom = today, DateTo = today });
        var tomorrow = await fixture.Service.GetMineAsync(new TransactionSearchObject { DateFrom = today.AddDays(1) });

        Assert.Equal(2, todayOnly.TotalCount);
        Assert.Equal(0, tomorrow.TotalCount);
    }

    [Fact]
    public async Task GetMine_CardFilter_OnlyMatchesMySideOfTransaction()
    {
        var (fixture, senderCard, receiverCard) = await HistoryAsync();

        var mine = await fixture.Service.GetMineAsync(new TransactionSearchObject { CardId = senderCard.Id });
        var theirs = await fixture.Service.GetMineAsync(new TransactionSearchObject { CardId = receiverCard.Id });

        Assert.Equal(2, mine.TotalCount);
        Assert.Equal(0, theirs.TotalCount);
    }

    [Fact]
    public async Task GetMine_WithReversedRanges_GivesFieldErrors()
    {
        var fixture = await NewFixtureAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => fixture.Service.GetMineAsync(
            new TransactionSearchObject { DateFrom = today, DateTo = today.AddDays(-1), AmountFrom = 10m, AmountTo = 5m }));

        Assert.Equal([TransactionService.DateRangeInvalid], exception.Errors["dateTo"]);
        Assert.Equal([TransactionService.AmountRangeInvalid], exception.Errors["amountTo"]);
    }

    [Fact]
    public async Task GetMineById_ShowsEachParticipantTheirOwnSide_AndHidesItFromOthers()
    {
        var (fixture, _, _) = await HistoryAsync();
        var id = await fixture.Context.Transactions.Where(t => t.SenderUserId == fixture.Sender.Id).Select(t => t.Id).SingleAsync();

        var asSender = await fixture.Service.GetMineByIdAsync(id);
        fixture.ActAs(fixture.Receiver);
        var asReceiver = await fixture.Service.GetMineByIdAsync(id);
        fixture.ActAs(await fixture.Context.Users.SingleAsync(u => u.Username == "treci"));

        Assert.Equal(TransactionDirection.Sent, asSender.Direction);
        Assert.Equal("primalac", asSender.CounterpartyLastName);
        Assert.Equal(TransactionDirection.Received, asReceiver.Direction);
        Assert.Equal("posiljalac", asReceiver.CounterpartyLastName);
        Assert.Equal("2222", asReceiver.MyCardLast4Digits);
        await Assert.ThrowsAsync<NotFoundException>(() => fixture.Service.GetMineByIdAsync(id));
    }
}
