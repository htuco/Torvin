using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Torvin.Model.Constants;
using Torvin.Model.Enums;
using Torvin.Model.Exceptions;
using Torvin.Model.Messages;
using Torvin.Model.Requests;
using Torvin.Model.SearchObjects;
using Torvin.Services.Database;
using Torvin.Services.Database.Entities;
using Torvin.Services.Security;
using Torvin.Services.Services;
using Torvin.Services.StateMachines;

namespace Torvin.Tests;

public partial class CardServiceTests
{
    private const string VisaNumber = "4111111111111111";
    private const string MastercardNumber = "5555555555554444";

    // Prijavljeni korisnik se mijenja unutar testa, da se provjeri pristup tuđoj kartici
    private sealed class TestCurrentUser : ICurrentUserService
    {
        public int UserId { get; set; }
        public string Role { get; set; } = AppRoles.User;
        public string TokenId => "test";
        public DateTime TokenExpiresAt => DateTime.UtcNow.AddHours(1);
    }

    private sealed class Fixture(TorvinDbContext context, User owner, User other, User admin)
    {
        public TorvinDbContext Context { get; } = context;
        public User Owner { get; } = owner;
        public User Other { get; } = other;
        public User Admin { get; } = admin;
        public TestCurrentUser CurrentUser { get; } = new() { UserId = owner.Id };
        public FakeMessagePublisher Publisher { get; } = new();

        public CardService Service => new(Context, CurrentUser, new EmailCodeService(Context), Publisher,
            NullLogger<CardService>.Instance);

        public void ActAs(User user, string role = AppRoles.User)
        {
            CurrentUser.UserId = user.Id;
            CurrentUser.Role = role;
        }
    }

    private static async Task<Fixture> NewFixtureAsync()
    {
        var context = new TorvinDbContext(TestDatabase.CreateOptions());
        await context.Database.EnsureCreatedAsync();
        var owner = await TestUsers.AddAsync(context, "vlasnik", AppRoles.User);
        var other = await TestUsers.AddAsync(context, "drugi", AppRoles.User);
        var admin = await TestUsers.AddAsync(context, "admin", AppRoles.Admin);
        return new Fixture(context, owner, other, admin);
    }

    private static async Task<Card> AddCardAsync(Fixture fixture, User user, string statusCode, decimal balance = 0,
        bool isDefault = false, string last4 = "4821")
    {
        var card = new Card
        {
            User = user,
            CardTypeId = 1,
            CurrencyId = 1,
            CardStatus = await fixture.Context.CardStatuses.SingleAsync(s => s.Code == statusCode),
            Network = CardNetwork.Visa,
            Last4Digits = last4,
            ExpiresAt = new DateOnly(2030, 12, 31),
            Balance = balance,
            IsDefault = isDefault,
            RowVersion = [1],
        };
        fixture.Context.Cards.Add(card);
        await fixture.Context.SaveChangesAsync();
        return card;
    }

    private static CardInsertRequest Insert(string number = VisaNumber, string? expiry = null) => new()
    {
        CardTypeId = 1,
        CurrencyId = 1,
        Name = "Putovanja",
        CardNumber = number,
        Expiry = expiry ?? Expiry(DateTime.UtcNow.AddYears(2)),
    };

    // Bez InvariantCulture bi "/" postao separator datuma lokalne kulture (npr. tačka)
    private static string Expiry(DateTime date) => date.ToString("MM/yy", CultureInfo.InvariantCulture);

    [GeneratedRegex(@"je (\d{6})\.")]
    private static partial Regex CodeInMail();

    private static string LastCode(Fixture fixture)
    {
        var mail = fixture.Publisher.Published.Select(p => p.Message).OfType<EmailRequested>().Last();
        return CodeInMail().Match(mail.Body).Groups[1].Value;
    }

    [Fact]
    public async Task Create_KeepsOnlyLast4AndNetwork_AndSendsCode()
    {
        var fixture = await NewFixtureAsync();
        var expiry = DateTime.UtcNow.AddYears(2);

        var response = await fixture.Service.CreateAsync(Insert(MastercardNumber, Expiry(expiry)));

        var card = await fixture.Context.Cards.AsNoTracking().SingleAsync(c => c.Id == response.Id);
        Assert.Equal(CardStatusCodes.Pending, response.CardStatusCode);
        Assert.Equal("4444", card.Last4Digits);
        Assert.Equal(CardNetwork.Mastercard, card.Network);
        Assert.Equal(new DateOnly(expiry.Year, expiry.Month, DateTime.DaysInMonth(expiry.Year, expiry.Month)), card.ExpiresAt);
        Assert.Equal(0m, card.Balance);
        Assert.False(card.IsDefault);

        // Pun broj ne smije završiti ni u bazi ni u mailu
        var token = await fixture.Context.EmailTokens.AsNoTracking().SingleAsync(t => t.CardId == card.Id);
        Assert.Equal(EmailTokenPurpose.CardActivation, token.Purpose);
        var mail = Assert.IsType<EmailRequested>(fixture.Publisher.Published.Single().Message);
        Assert.DoesNotContain(MastercardNumber, mail.Body);
        Assert.Contains("•••• 4444", mail.Body);
        Assert.DoesNotContain(MastercardNumber, token.CodeHash);
        Assert.Equal(QueueNames.Email, fixture.Publisher.Published.Single().Queue);
    }

    [Theory]
    [InlineData("01/20", true)]
    [InlineData("12/99", false)]
    public async Task Create_WithInvalidExpiry_IsRejectedOnField(string expiry, bool expired)
    {
        var fixture = await NewFixtureAsync();
        var message = expired ? CardService.CardExpired : CardService.ExpiryTooFar;

        var exception = await Assert.ThrowsAsync<ValidationException>(() => fixture.Service.CreateAsync(Insert(expiry: expiry)));

        Assert.Equal([message], exception.Errors["expiry"]);
        Assert.Empty(fixture.Context.Cards);
    }

    // Kartica je snimljena prije objave maila, pa pad reda ne smije javiti da dodavanje nije uspjelo
    [Fact]
    public async Task Create_WhenMailCannotBePublished_StillReturnsCard()
    {
        var fixture = await NewFixtureAsync();
        fixture.Publisher.OnPublish = _ => throw new InvalidOperationException("RabbitMQ nije dostupan.");

        var response = await fixture.Service.CreateAsync(Insert());

        Assert.Equal(CardStatusCodes.Pending, response.CardStatusCode);
        Assert.Single(fixture.Context.Cards);
    }

    [Fact]
    public async Task Create_SameCardTwice_IsRejected()
    {
        var fixture = await NewFixtureAsync();
        await fixture.Service.CreateAsync(Insert());

        var exception = await Assert.ThrowsAsync<BusinessException>(() => fixture.Service.CreateAsync(Insert()));

        Assert.Equal("Kartica •••• 1111 je već dodana.", exception.Message);
    }

    [Fact]
    public async Task Create_SixthOpenCard_IsRejected_ButReportedCardsDoNotCount()
    {
        var fixture = await NewFixtureAsync();
        for (var i = 0; i < CardService.MaxOpenCards; i++)
        {
            await AddCardAsync(fixture, fixture.Owner, CardStatusCodes.Active, last4: $"000{i}");
        }

        await AddCardAsync(fixture, fixture.Owner, CardStatusCodes.Stolen, last4: "9999");

        var exception = await Assert.ThrowsAsync<BusinessException>(() => fixture.Service.CreateAsync(Insert()));

        Assert.Equal(CardService.TooManyCards, exception.Message);
    }

    [Fact]
    public async Task Verify_WithCorrectCode_ActivatesSetsDefaultAndPublishes()
    {
        var fixture = await NewFixtureAsync();
        var created = await fixture.Service.CreateAsync(Insert());

        var response = await fixture.Service.VerifyAsync(created.Id, new CardVerifyRequest { Code = LastCode(fixture) });

        Assert.Equal(CardStatusCodes.Active, response.CardStatusCode);
        Assert.True(response.IsDefault);
        var (queue, message) = fixture.Publisher.Published.Last();
        var changed = Assert.IsType<CardStatusChanged>(message);
        Assert.Equal(QueueNames.CardStatus, queue);
        Assert.Equal((created.Id, CardStatusCodes.Pending, CardStatusCodes.Active, fixture.Owner.Id),
            (changed.CardId, changed.OldStatusCode, changed.NewStatusCode, changed.ChangedByUserId));
        var audit = await fixture.Context.AuditLogs.SingleAsync(a => a.EntityId == created.Id);
        Assert.Equal(AuditActions.CardStatusChanged, audit.Action);
        Assert.Equal("""{"Status":"Pending"}""", audit.OldValues);
        Assert.Equal("""{"Status":"Active"}""", audit.NewValues);
    }

    [Fact]
    public async Task Verify_SecondCard_DoesNotTakeDefaultFromFirst()
    {
        var fixture = await NewFixtureAsync();
        await AddCardAsync(fixture, fixture.Owner, CardStatusCodes.Active, isDefault: true);
        var created = await fixture.Service.CreateAsync(Insert());

        var response = await fixture.Service.VerifyAsync(created.Id, new CardVerifyRequest { Code = LastCode(fixture) });

        Assert.False(response.IsDefault);
    }

    [Fact]
    public async Task Verify_WithCodeOfAnotherCard_IsRejected()
    {
        var fixture = await NewFixtureAsync();
        var first = await fixture.Service.CreateAsync(Insert(VisaNumber));
        var firstCode = LastCode(fixture);
        var second = await fixture.Service.CreateAsync(Insert(MastercardNumber));

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            fixture.Service.VerifyAsync(second.Id, new CardVerifyRequest { Code = firstCode }));

        Assert.Equal(AuthService.InvalidCode, exception.Message);
        // Kod druge kartice nije poništio kod prve
        var response = await fixture.Service.VerifyAsync(first.Id, new CardVerifyRequest { Code = firstCode });
        Assert.Equal(CardStatusCodes.Active, response.CardStatusCode);
    }

    [Fact]
    public async Task OtherUsersCard_IsNotFound_ForEveryOperation()
    {
        var fixture = await NewFixtureAsync();
        var card = await AddCardAsync(fixture, fixture.Other, CardStatusCodes.Active);
        var service = fixture.Service;

        // Operacije idu jedna po jedna, jer dijele isti DbContext
        Func<Task>[] operations =
        [
            () => service.GetMineByIdAsync(card.Id),
            () => service.VerifyAsync(card.Id, new CardVerifyRequest { Code = "123456" }),
            () => service.ResendCodeAsync(card.Id),
            () => service.BlockAsync(card.Id),
            () => service.UnblockAsync(card.Id),
            () => service.ReportAsync(card.Id, new CardReportRequest { Reason = CardReportReason.Stolen }),
        ];

        foreach (var operation in operations)
        {
            var exception = await Assert.ThrowsAsync<NotFoundException>(operation);
            Assert.Equal(CardService.NotFound, exception.Message);
        }

        Assert.Equal(CardStatusCodes.Active, (await fixture.Context.Cards.Include(c => c.CardStatus).SingleAsync()).CardStatus.Code);
    }

    [Fact]
    public async Task GetMine_ReturnsOnlyOwnCards_NewestFirst()
    {
        var fixture = await NewFixtureAsync();
        await AddCardAsync(fixture, fixture.Owner, CardStatusCodes.Active, last4: "0001");
        await AddCardAsync(fixture, fixture.Other, CardStatusCodes.Active, last4: "0002");
        await AddCardAsync(fixture, fixture.Owner, CardStatusCodes.Blocked, last4: "0003");

        var page = await fixture.Service.GetMineAsync(new() { PageSize = 10 });

        Assert.Equal(["0003", "0001"], page.Items.Select(c => c.Last4Digits));
        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task BlockThenUnblock_ByOwner_WritesAuditForBoth()
    {
        var fixture = await NewFixtureAsync();
        var card = await AddCardAsync(fixture, fixture.Owner, CardStatusCodes.Active);

        var blocked = await fixture.Service.BlockAsync(card.Id);
        var unblocked = await fixture.Service.UnblockAsync(card.Id);

        Assert.Equal(CardStatusCodes.Blocked, blocked.CardStatusCode);
        Assert.Equal(CardStatusCodes.Active, unblocked.CardStatusCode);
        Assert.Equal([AuditActions.CardBlocked, AuditActions.CardStatusChanged],
            fixture.Context.AuditLogs.OrderBy(a => a.Id).Select(a => a.Action));
        Assert.Equal(2, fixture.Publisher.Published.Count(p => p.Queue == QueueNames.CardStatus));
    }

    [Fact]
    public async Task Unblock_CardBlockedByAdmin_IsRejected()
    {
        var fixture = await NewFixtureAsync();
        var card = await AddCardAsync(fixture, fixture.Owner, CardStatusCodes.Active);
        fixture.ActAs(fixture.Admin, AppRoles.Admin);
        var blocked = await fixture.Service.ChangeStatusAsync(card.Id,
            new CardStatusChangeRequest { CardStatusId = await StatusIdAsync(fixture, CardStatusCodes.Blocked), Reason = "Sumnjiva aktivnost." });
        fixture.ActAs(fixture.Owner);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => fixture.Service.UnblockAsync(card.Id));

        Assert.Equal(CardStatusCodes.Blocked, blocked.CardStatusCode);
        Assert.Equal(CardService.BlockedByAdmin, exception.Message);
        // Mobilna iz ovoga zna da Deblokiraj ne smije biti dostupno, u detaljima i u listi
        Assert.True(blocked.IsBlockedByAdmin);
        Assert.True((await fixture.Service.GetMineByIdAsync(card.Id)).IsBlockedByAdmin);
        Assert.True((await fixture.Service.GetMineAsync(new CardSearchObject())).Items.Single().IsBlockedByAdmin);
        fixture.ActAs(fixture.Admin, AppRoles.Admin);
        Assert.True((await fixture.Service.GetAllAsync(new AdminCardSearchObject())).Items.Single().IsBlockedByAdmin);
    }

    [Fact]
    public async Task BlockedByOwner_IsNotMarkedAsBlockedByAdmin()
    {
        var fixture = await NewFixtureAsync();
        var card = await AddCardAsync(fixture, fixture.Owner, CardStatusCodes.Active);

        var blocked = await fixture.Service.BlockAsync(card.Id);

        Assert.False(blocked.IsBlockedByAdmin);
        Assert.False((await fixture.Service.GetMineByIdAsync(card.Id)).IsBlockedByAdmin);
        Assert.Equal(CardStatusCodes.Active, (await fixture.Service.UnblockAsync(card.Id)).CardStatusCode);
    }

    [Fact]
    public async Task OwnerBlock_AfterAdminUnblocked_IsNotMarkedAsBlockedByAdmin()
    {
        var fixture = await NewFixtureAsync();
        var card = await AddCardAsync(fixture, fixture.Owner, CardStatusCodes.Active);
        fixture.ActAs(fixture.Admin, AppRoles.Admin);
        await fixture.Service.ChangeStatusAsync(card.Id,
            new CardStatusChangeRequest { CardStatusId = await StatusIdAsync(fixture, CardStatusCodes.Blocked), Reason = "Provjera." });
        await fixture.Service.ChangeStatusAsync(card.Id,
            new CardStatusChangeRequest { CardStatusId = await StatusIdAsync(fixture, CardStatusCodes.Active), Reason = "Provjereno." });
        fixture.ActAs(fixture.Owner);

        var blocked = await fixture.Service.BlockAsync(card.Id);

        // Odlučuje posljednje blokiranje, a njega je napravio vlasnik
        Assert.False(blocked.IsBlockedByAdmin);
        Assert.Equal(CardStatusCodes.Active, (await fixture.Service.UnblockAsync(card.Id)).CardStatusCode);
    }

    [Fact]
    public async Task Report_OfCardBlockedByAdmin_IsAllowed_AndNoLongerMarked()
    {
        var fixture = await NewFixtureAsync();
        var card = await AddCardAsync(fixture, fixture.Owner, CardStatusCodes.Active);
        fixture.ActAs(fixture.Admin, AppRoles.Admin);
        await fixture.Service.ChangeStatusAsync(card.Id,
            new CardStatusChangeRequest { CardStatusId = await StatusIdAsync(fixture, CardStatusCodes.Blocked), Reason = "Provjera." });
        fixture.ActAs(fixture.Owner);

        var reported = await fixture.Service.ReportAsync(card.Id, new CardReportRequest { Reason = CardReportReason.Lost });

        // Oznaka važi samo dok je kartica blokirana, a prijava gubitka je dozvoljena i poslije blokade administratora
        Assert.Equal(CardStatusCodes.Lost, reported.CardStatusCode);
        Assert.False(reported.IsBlockedByAdmin);
    }

    [Fact]
    public async Task Report_Stolen_IsFinal_AndClearsDefault()
    {
        var fixture = await NewFixtureAsync();
        var card = await AddCardAsync(fixture, fixture.Owner, CardStatusCodes.Active, isDefault: true);

        var reported = await fixture.Service.ReportAsync(card.Id,
            new CardReportRequest { Reason = CardReportReason.Stolen, Note = "Ukradena u tramvaju." });
        var exception = await Assert.ThrowsAsync<BusinessException>(() => fixture.Service.UnblockAsync(card.Id));

        Assert.Equal(CardStatusCodes.Stolen, reported.CardStatusCode);
        Assert.False(reported.IsDefault);
        Assert.Equal(CardStateMachine.StolenCannotBeActivated, exception.Message);
        var changed = fixture.Publisher.Published.Select(p => p.Message).OfType<CardStatusChanged>().Single();
        Assert.Equal("Ukradena u tramvaju.", changed.Reason);
    }

    [Fact]
    public async Task Admin_ClosingCardWithBalance_IsRejected_AndWithZeroBalanceAllowed()
    {
        var fixture = await NewFixtureAsync();
        var withMoney = await AddCardAsync(fixture, fixture.Owner, CardStatusCodes.Blocked, balance: 10m, last4: "0001");
        var empty = await AddCardAsync(fixture, fixture.Owner, CardStatusCodes.Lost, last4: "0002");
        fixture.ActAs(fixture.Admin, AppRoles.Admin);
        var closed = await StatusIdAsync(fixture, CardStatusCodes.Closed);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => fixture.Service.ChangeStatusAsync(withMoney.Id,
            new CardStatusChangeRequest { CardStatusId = closed, Reason = "Zahtjev korisnika." }));
        var response = await fixture.Service.ChangeStatusAsync(empty.Id,
            new CardStatusChangeRequest { CardStatusId = closed, Reason = "Zahtjev korisnika." });

        Assert.Equal(CardService.CloseNeedsZeroBalance, exception.Message);
        Assert.Equal(CardStatusCodes.Closed, response.CardStatusCode);
        var audit = await fixture.Context.AuditLogs.SingleAsync();
        Assert.Equal((fixture.Admin.Id, "Zahtjev korisnika."), (audit.ActorUserId, audit.Description));
    }

    [Fact]
    public async Task Admin_ReasonOfOnlySpaces_IsRejectedOnField()
    {
        var fixture = await NewFixtureAsync();
        var card = await AddCardAsync(fixture, fixture.Owner, CardStatusCodes.Active);
        fixture.ActAs(fixture.Admin, AppRoles.Admin);
        var blocked = await StatusIdAsync(fixture, CardStatusCodes.Blocked);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => fixture.Service.ChangeStatusAsync(card.Id,
            new CardStatusChangeRequest { CardStatusId = blocked, Reason = "    a" }));

        Assert.Equal([CardStatusChangeRequest.ReasonLengthMessage], exception.Errors["reason"]);
        Assert.Empty(fixture.Context.AuditLogs);
    }

    [Fact]
    public async Task Admin_UnknownStatus_IsRejectedOnField()
    {
        var fixture = await NewFixtureAsync();
        var card = await AddCardAsync(fixture, fixture.Owner, CardStatusCodes.Active);
        fixture.ActAs(fixture.Admin, AppRoles.Admin);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => fixture.Service.ChangeStatusAsync(card.Id,
            new CardStatusChangeRequest { CardStatusId = 999, Reason = "Provjera statusa." }));

        Assert.Equal([CardService.StatusMissing], exception.Errors["cardStatusId"]);
    }

    [Fact]
    public async Task Admin_SeesCardsOfAllUsers_FilteredByUser()
    {
        var fixture = await NewFixtureAsync();
        await AddCardAsync(fixture, fixture.Owner, CardStatusCodes.Active, last4: "0001");
        await AddCardAsync(fixture, fixture.Other, CardStatusCodes.Active, last4: "0002");
        fixture.ActAs(fixture.Admin, AppRoles.Admin);

        var all = await fixture.Service.GetAllAsync(new());
        var ofOther = await fixture.Service.GetAllAsync(new() { UserId = fixture.Other.Id });

        Assert.Equal(2, all.TotalCount);
        Assert.Equal("drugi", ofOther.Items.Single().UserLastName);
    }

    private static Task<int> StatusIdAsync(Fixture fixture, string code) =>
        fixture.Context.CardStatuses.Where(s => s.Code == code).Select(s => s.Id).SingleAsync();
}
