using System.Globalization;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Torvin.Model.Constants;
using Torvin.Model.Enums;
using Torvin.Model.Exceptions;
using Torvin.Model.Messages;
using Torvin.Model.Requests;
using Torvin.Model.Responses;
using Torvin.Model.SearchObjects;
using Torvin.Model.Validation;
using Torvin.Services.Database;
using Torvin.Services.Database.Entities;
using Torvin.Services.Messaging;
using Torvin.Services.Security;

namespace Torvin.Services.Services;

public interface ITransactionService
{
    Task<TransactionResponse> CreateAsync(TransactionInsertRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<TransactionResponse>> GetMineAsync(TransactionSearchObject search,
        CancellationToken cancellationToken = default);

    Task<TransactionResponse> GetMineByIdAsync(int id, CancellationToken cancellationToken = default);
}

// Interni prenos s kartice prijavljenog korisnika na karticu drugog korisnika, i historija iz pogleda učesnika.
// Prenos je u valuti kartice pošiljaoca, a primalac mora imati aktivnu karticu u istoj valuti
public sealed class TransactionService(
    TorvinDbContext context,
    ICurrentUserService currentUser,
    IMessagePublisher publisher,
    ILogger<TransactionService> logger) : ITransactionService
{
    public const string CardNotFound = "Kartica nije pronađena.";
    public const string CannotSendToSelf = "Ne možete poslati novac sebi.";
    public const string RecipientNotFound = "Primalac nije pronađen.";
    public const string CategoryMissing = "Odabrana kategorija ne postoji.";
    public const string NotFound = "Transakcija nije pronađena.";
    public const string DateRangeInvalid = "Datum do ne može biti prije datuma od.";
    public const string AmountRangeInvalid = "Iznos do ne može biti manji od iznosa od.";

    public static string CardNotActive(string last4) => $"Kartica •••• {last4} nije aktivna.";

    public static string RecipientHasNoCard(string currencyCode) => $"Primalac nema aktivnu karticu u valuti {currencyCode}.";

    public static string InsufficientFunds(string last4) => $"Nedovoljno sredstava na kartici •••• {last4}.";

    public async Task<TransactionResponse> CreateAsync(TransactionInsertRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;

        // Provjere se ponavljaju i ovdje, jer servis mogu zvati i testovi i drugi servisi bez validacije modela
        if (!Money.IsValidAmount(request.Amount))
        {
            throw FieldError("amount", Money.InvalidAmount);
        }

        // Token blokiranog korisnika važi do isteka, pa se račun provjerava i ovdje
        if (!await context.Users.AnyAsync(u => u.Id == userId && u.IsActive, cancellationToken))
        {
            throw new ForbiddenException(AuthService.AccountBlocked);
        }

        // Dva snimanja (prenos, pa audit s Id-em transakcije) su jedna cjelina. Bez Commit se oba vraćaju
        await using var dbTransaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var fromCard = await context.Cards.Include(c => c.CardStatus).Include(c => c.Currency)
            .FirstOrDefaultAsync(c => c.Id == request.FromCardId && c.UserId == userId, cancellationToken)
            ?? throw FieldError("fromCardId", CardNotFound);
        if (fromCard.CardStatus.Code != CardStatusCodes.Active)
        {
            throw FieldError("fromCardId", CardNotActive(fromCard.Last4Digits));
        }

        var toCard = await FindRecipientCardAsync(userId, request.ToUserId, fromCard, cancellationToken);

        if (request.TransactionCategoryId is not null
            && !await context.TransactionCategories.AnyAsync(c => c.Id == request.TransactionCategoryId, cancellationToken))
        {
            throw FieldError("transactionCategoryId", CategoryMissing);
        }

        if (fromCard.Balance < request.Amount)
        {
            throw FieldError("amount", InsufficientFunds(fromCard.Last4Digits));
        }

        var oldValues = Balances(fromCard, toCard);
        fromCard.Balance -= request.Amount;
        toCard.Balance += request.Amount;

        var transaction = new Transaction
        {
            ReferenceNumber = TransactionReference.New(DateTime.UtcNow),
            SenderUserId = userId,
            ReceiverUserId = toCard.UserId,
            SenderCardId = fromCard.Id,
            ReceiverCardId = toCard.Id,
            Amount = request.Amount,
            CurrencyId = fromCard.CurrencyId,
            TransactionType = await context.TransactionTypes
                .SingleAsync(t => t.Code == TransactionTypeCodes.Transfer, cancellationToken),
            TransactionStatus = await context.TransactionStatuses
                .SingleAsync(s => s.Code == TransactionStatusCodes.Completed, cancellationToken),
            TransactionCategoryId = request.TransactionCategoryId,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
        };
        context.Transactions.Add(transaction);
        await SaveAsync(cancellationToken);

        context.AuditLogs.Add(new AuditLog
        {
            ActorUserId = userId,
            Action = AuditActions.TransactionCompleted,
            EntityType = nameof(Transaction),
            EntityId = transaction.Id,
            OldValues = oldValues,
            NewValues = Balances(fromCard, toCard),
            Description = string.Create(CultureInfo.InvariantCulture,
                $"Prenos {request.Amount:0.00} {fromCard.Currency.Code} s kartice •••• {fromCard.Last4Digits} na karticu •••• {toCard.Last4Digits}."),
        });
        await SaveAsync(cancellationToken);
        await dbTransaction.CommitAsync(cancellationToken);

        await PublishCompletedAsync(transaction, fromCard.Currency.Code, cancellationToken);
        return await GetMineByIdAsync(transaction.Id, cancellationToken);
    }

    public Task<PagedResult<TransactionResponse>> GetMineAsync(TransactionSearchObject search,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        return Filter(Mine(userId), search, userId)
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .Select(ToResponse(userId))
            .ToPagedResultAsync(search, cancellationToken);
    }

    public async Task<TransactionResponse> GetMineByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        return await Mine(userId)
            .Where(t => t.Id == id)
            .Select(ToResponse(userId))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(NotFound);
    }

    // Primalac je drugi aktivan korisnik. Za nepostojeći, blokiran, admin i nepotvrđen račun poruka je ista, da se ne
    // otkriva stanje tuđeg računa. Novac ide na podrazumijevanu karticu ako je aktivna i u istoj valuti, inače na
    // najstariju takvu karticu
    private async Task<Card> FindRecipientCardAsync(int userId, int toUserId, Card fromCard,
        CancellationToken cancellationToken)
    {
        if (toUserId == userId)
        {
            throw FieldError("toUserId", CannotSendToSelf);
        }

        if (!await context.Users.AnyAsync(u => u.Id == toUserId && u.IsActive && u.EmailConfirmed
                && u.Role.Name == AppRoles.User, cancellationToken))
        {
            throw FieldError("toUserId", RecipientNotFound);
        }

        return await context.Cards
            .Where(c => c.UserId == toUserId && c.CurrencyId == fromCard.CurrencyId
                && c.CardStatus.Code == CardStatusCodes.Active)
            .OrderByDescending(c => c.IsDefault)
            .ThenBy(c => c.CreatedAt)
            .ThenBy(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw FieldError("toUserId", RecipientHasNoCard(fromCard.Currency.Code));
    }

    // Dva istovremena prenosa nad istom karticom: drugi naiđe na RowVersion i dobija poruku umjesto greške servera.
    // Transakcija se tada vraća, jer Commit nije pozvan
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new BusinessException(CardService.Conflict);
        }
    }

    // Prenos je već knjižen, pa greška reda ne smije korisniku javiti da prenos nije uspio. Ostaje u logu
    private async Task PublishCompletedAsync(Transaction transaction, string currencyCode, CancellationToken cancellationToken)
    {
        var message = new TransactionCompleted(Guid.NewGuid(), transaction.Id, transaction.SenderUserId,
            transaction.ReceiverUserId, transaction.Amount, currencyCode, transaction.TransactionCategoryId, transaction.CreatedAt);
        try
        {
            await publisher.PublishEventAsync(ExchangeNames.TransactionCompleted, message, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Transakcija {TransactionId} nije objavljena na exchange.", transaction.Id);
        }

        logger.LogInformation("Transakcija {TransactionId} knjižena: korisnik {SenderUserId} → korisnik {ReceiverUserId}.",
            transaction.Id, transaction.SenderUserId, transaction.ReceiverUserId);
    }

    // Moje transakcije: sve koje sam poslao i uspjele koje sam primio. Neuspjeli prenos primaocu ništa nije promijenio
    private IQueryable<Transaction> Mine(int userId) =>
        context.Transactions.AsNoTracking().Where(t => t.SenderUserId == userId
            || (t.ReceiverUserId == userId && t.TransactionStatus.Code != TransactionStatusCodes.Failed));

    private static IQueryable<Transaction> Filter(IQueryable<Transaction> query, TransactionSearchObject search, int userId)
    {
        var errors = new Dictionary<string, string[]>();
        if (search.DateFrom > search.DateTo)
        {
            errors["dateTo"] = [DateRangeInvalid];
        }

        if (search.AmountFrom > search.AmountTo)
        {
            errors["amountTo"] = [AmountRangeInvalid];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        if (search.DateFrom is not null)
        {
            var from = search.DateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(t => t.CreatedAt >= from);
        }

        // Datum do je uključiv: sve prije ponoći sljedećeg dana
        if (search.DateTo is not null)
        {
            var to = search.DateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(t => t.CreatedAt < to);
        }

        if (search.TransactionTypeId is not null)
        {
            query = query.Where(t => t.TransactionTypeId == search.TransactionTypeId);
        }

        if (search.TransactionStatusId is not null)
        {
            query = query.Where(t => t.TransactionStatusId == search.TransactionStatusId);
        }

        // Samo moja strana transakcije, da filter ne otkriva tuđe kartice
        if (search.CardId is not null)
        {
            query = query.Where(t => (t.SenderUserId == userId && t.SenderCardId == search.CardId)
                || (t.ReceiverUserId == userId && t.ReceiverCardId == search.CardId));
        }

        if (search.AmountFrom is not null)
        {
            query = query.Where(t => t.Amount >= search.AmountFrom);
        }

        if (search.AmountTo is not null)
        {
            query = query.Where(t => t.Amount <= search.AmountTo);
        }

        if (search.Direction == TransactionDirection.Sent)
        {
            query = query.Where(t => t.SenderUserId == userId);
        }
        else if (search.Direction == TransactionDirection.Received)
        {
            query = query.Where(t => t.ReceiverUserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(search.Fts))
        {
            var fts = search.Fts.Trim();
            query = query.Where(t => (t.Note != null && t.Note.Contains(fts))
                || t.ReferenceNumber.Contains(fts)
                || (t.SenderUserId == userId
                    ? t.ReceiverUser.FirstName.Contains(fts) || t.ReceiverUser.LastName.Contains(fts)
                        || t.ReceiverUser.Username.Contains(fts)
                    : t.SenderUser.FirstName.Contains(fts) || t.SenderUser.LastName.Contains(fts)
                        || t.SenderUser.Username.Contains(fts)));
        }

        return query;
    }

    // Polja zavise od toga ko gleda (smjer, moja kartica, druga strana), pa projekcija ide ručno, i dalje u jednom upitu
    private static Expression<Func<Transaction, TransactionResponse>> ToResponse(int userId) => t => new TransactionResponse(
        t.Id,
        t.ReferenceNumber,
        t.SenderUserId == userId ? TransactionDirection.Sent : TransactionDirection.Received,
        t.Amount,
        t.Currency.Code,
        t.TransactionType.Code,
        t.TransactionType.Name,
        t.TransactionStatus.Code,
        t.TransactionStatus.Name,
        t.TransactionCategoryId,
        t.TransactionCategory != null ? t.TransactionCategory.Name : null,
        t.Note,
        t.SenderUserId == userId ? t.SenderCardId : t.ReceiverCardId,
        t.SenderUserId == userId
            ? (t.SenderCard != null ? t.SenderCard.Last4Digits : null)
            : (t.ReceiverCard != null ? t.ReceiverCard.Last4Digits : null),
        t.SenderUserId == userId ? t.ReceiverUserId : t.SenderUserId,
        t.SenderUserId == userId ? t.ReceiverUser.FirstName : t.SenderUser.FirstName,
        t.SenderUserId == userId ? t.ReceiverUser.LastName : t.SenderUser.LastName,
        t.SenderUserId == userId
            ? (t.ReceiverCard != null ? t.ReceiverCard.Last4Digits : null)
            : (t.SenderCard != null ? t.SenderCard.Last4Digits : null),
        t.CreatedAt);

    private static string Balances(Card fromCard, Card toCard) => string.Create(CultureInfo.InvariantCulture,
        $$"""{"FromCardBalance":"{{fromCard.Balance:0.00}}","ToCardBalance":"{{toCard.Balance:0.00}}"}""");

    private static ValidationException FieldError(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
