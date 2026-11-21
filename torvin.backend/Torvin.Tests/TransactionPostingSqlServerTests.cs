using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Torvin.Model.Constants;
using Torvin.Model.Exceptions;
using Torvin.Model.Messages;
using Torvin.Model.Requests;
using Torvin.Services.Database;
using Torvin.Services.Database.Entities;
using Torvin.Services.Security;
using Torvin.Services.Services;

namespace Torvin.Tests;

// Knjiženje nad pravim SQL Serverom: sve ili ništa, objava tek poslije commita i dva istovremena prenosa.
// Istovremenost se pravi deterministički: interceptor pokrene drugi prenos tačno prije prvog snimanja
[Collection(SqlServerCollection.Name)]
[Trait("Category", "SqlServer")]
public class TransactionPostingSqlServerTests(SqlServerFixture database)
{
    private sealed class FixedCurrentUser(int userId) : ICurrentUserService
    {
        public int UserId { get; } = userId;
        public string Role => AppRoles.User;
        public string TokenId => "test";
        public DateTime TokenExpiresAt => DateTime.UtcNow.AddHours(1);
    }

    // Obori snimanje audit zapisa, tj. drugi korak knjiženja, poslije prvog koji je već upisao balanse i transakciju
    private sealed class FailOnAuditSave : SaveChangesInterceptor
    {
        public int SavesBeforeFailure { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<AuditLog>().Any(e => e.State == EntityState.Added))
            {
                throw new DbUpdateException("Simuliran pad drugog koraka.");
            }

            SavesBeforeFailure++;
            return ValueTask.FromResult(result);
        }
    }

    private static TransactionService Service(TorvinDbContext context, User user, FakeMessagePublisher? publisher = null) =>
        new(context, new FixedCurrentUser(user.Id), publisher ?? new FakeMessagePublisher(),
            NullLogger<TransactionService>.Instance);

    private static TransactionInsertRequest Send(Card from, User to, decimal amount) =>
        new() { FromCardId = from.Id, ToUserId = to.Id, Amount = amount };

    private async Task<(User User, Card Card)> UserWithCardAsync(decimal balance)
    {
        await using var context = database.CreateContext();
        var user = await TestUsers.AddAsync(context, $"sql.{Guid.NewGuid():N}"[..30], AppRoles.User);
        var card = await TestCards.AddAsync(context, user, balance, isDefault: true);
        return (user, card);
    }

    private async Task<decimal> BalanceAsync(Card card)
    {
        await using var context = database.CreateContext();
        return await context.Cards.Where(c => c.Id == card.Id).Select(c => c.Balance).SingleAsync();
    }

    private async Task<int> SentCountAsync(User user)
    {
        await using var context = database.CreateContext();
        return await context.Transactions.CountAsync(t => t.SenderUserId == user.Id);
    }

    [Fact]
    public async Task Transfer_IsCommitted_BeforeEventIsPublished()
    {
        var (sender, from) = await UserWithCardAsync(100m);
        var (receiver, to) = await UserWithCardAsync(0m);
        var publisher = new FakeMessagePublisher();
        var committedAtPublish = -1;

        // READPAST preskače zaključane (nepotvrđene) redove: vidi transakciju samo ako je već commitovana
        publisher.OnPublish = async message =>
        {
            var id = ((TransactionCompleted)message).TransactionId;
            await using var other = database.CreateContext();
            committedAtPublish = await other.Database
                .SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM [Transactions] WITH (READPAST) WHERE [Id] = {id}")
                .SingleAsync();
        };

        await using var context = database.CreateContext();
        await Service(context, sender, publisher).CreateAsync(Send(from, receiver, 30m));

        Assert.Equal(1, committedAtPublish);
        Assert.Equal(70m, await BalanceAsync(from));
        Assert.Equal(30m, await BalanceAsync(to));
        await using var check = database.CreateContext();
        var transaction = await check.Transactions.SingleAsync(t => t.SenderUserId == sender.Id);
        Assert.True(await check.AuditLogs.AnyAsync(a => a.Action == AuditActions.TransactionCompleted
            && a.EntityId == transaction.Id));
    }

    [Fact]
    public async Task WhenSecondStepFails_EverythingIsRolledBack()
    {
        var (sender, from) = await UserWithCardAsync(100m);
        var (receiver, to) = await UserWithCardAsync(0m);
        var failOnAudit = new FailOnAuditSave();
        var publisher = new FakeMessagePublisher();

        await using var context = database.CreateContext(failOnAudit);
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => Service(context, sender, publisher).CreateAsync(Send(from, receiver, 30m)));

        // Prvi korak je stvarno upisan u bazu, a ipak nema traga od njega
        Assert.Equal(CardService.Conflict, exception.Message);
        Assert.Equal(1, failOnAudit.SavesBeforeFailure);
        Assert.Equal(100m, await BalanceAsync(from));
        Assert.Equal(0m, await BalanceAsync(to));
        Assert.Equal(0, await SentCountAsync(sender));
        Assert.Empty(publisher.Published);
    }

    [Fact]
    public async Task TwoTransfersFromSameCard_OnlyOneSpendsTheBalance()
    {
        var (sender, from) = await UserWithCardAsync(100m);
        var (receiver, to) = await UserWithCardAsync(0m);

        // Oba prenosa pročitaju balans 100, a drugi se snimi prvi. Zajedno bi tražili 140 od 100
        var competing = new BeforeFirstSave(async () =>
        {
            await using var other = database.CreateContext();
            await Service(other, sender).CreateAsync(Send(from, receiver, 70m));
        });
        await using var context = database.CreateContext(competing);
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => Service(context, sender).CreateAsync(Send(from, receiver, 70m)));

        Assert.Equal(CardService.Conflict, exception.Message);
        Assert.Equal(30m, await BalanceAsync(from));
        Assert.Equal(70m, await BalanceAsync(to));
        Assert.Equal(1, await SentCountAsync(sender));
    }

    [Fact]
    public async Task TwoTransfersToSameCard_DoNotOverwriteEachOther()
    {
        var (first, firstCard) = await UserWithCardAsync(100m);
        var (second, secondCard) = await UserWithCardAsync(100m);
        var (receiver, to) = await UserWithCardAsync(0m);

        var competing = new BeforeFirstSave(async () =>
        {
            await using var other = database.CreateContext();
            await Service(other, second).CreateAsync(Send(secondCard, receiver, 40m));
        });
        await using var context = database.CreateContext(competing);
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => Service(context, first).CreateAsync(Send(firstCard, receiver, 25m)));

        // Bez RowVersion bi drugi upis pregazio balans primaoca (0 + 25 umjesto 40 + 25)
        Assert.Equal(CardService.Conflict, exception.Message);
        Assert.Equal(100m, await BalanceAsync(firstCard));
        Assert.Equal(60m, await BalanceAsync(secondCard));
        Assert.Equal(40m, await BalanceAsync(to));
    }
}
