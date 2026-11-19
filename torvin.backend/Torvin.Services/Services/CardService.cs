using System.Globalization;
using Mapster;
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
using Torvin.Services.StateMachines;

namespace Torvin.Services.Services;

public interface ICardService
{
    Task<PagedResult<CardResponse>> GetMineAsync(CardSearchObject search, CancellationToken cancellationToken = default);

    Task<CardResponse> GetMineByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<CardResponse> CreateAsync(CardInsertRequest request, CancellationToken cancellationToken = default);

    Task<CardResponse> VerifyAsync(int id, CardVerifyRequest request, CancellationToken cancellationToken = default);

    Task ResendCodeAsync(int id, CancellationToken cancellationToken = default);

    Task<CardResponse> BlockAsync(int id, CancellationToken cancellationToken = default);

    Task<CardResponse> UnblockAsync(int id, CancellationToken cancellationToken = default);

    Task<CardResponse> ReportAsync(int id, CardReportRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<CardResponse>> GetAllAsync(AdminCardSearchObject search, CancellationToken cancellationToken = default);

    Task<CardResponse> ChangeStatusAsync(int id, CardStatusChangeRequest request, CancellationToken cancellationToken = default);
}

// Kartice korisnika i ručna promjena statusa od strane admina. Svaki prelaz ide kroz CardStateMachine, ostavlja
// audit zapis u istom snimanju i poslije snimanja šalje CardStatusChanged
public sealed class CardService(
    TorvinDbContext context,
    ICurrentUserService currentUser,
    IEmailCodeService codes,
    IMessagePublisher publisher,
    ILogger<CardService> logger) : ICardService
{
    // Pravilo duplikata: korisnik ne gomila kartice, a izgubljene, ukradene i zatvorene se ne broje
    public const int MaxOpenCards = 5;
    public const int MaxExpiryYears = 10;

    public const string NotFound = "Kartica nije pronađena.";
    public static readonly string TooManyCards = $"Možete imati najviše {MaxOpenCards} kartica koje nisu zatvorene ni prijavljene.";
    public const string CardExpired = "Kartica je istekla. Unesite karticu koja još važi.";
    public static readonly string ExpiryTooFar = $"Datum isteka može biti najviše {MaxExpiryYears} godina unaprijed.";
    public const string CardTypeMissing = "Odabrani tip kartice ne postoji.";
    public const string CurrencyMissing = "Odabrana valuta ne postoji.";
    public const string StatusMissing = "Odabrani status ne postoji.";
    public const string CodeOnlyForPending = "Kod se šalje samo za karticu koja čeka aktivaciju.";
    public static readonly string ResendTooSoon =
        $"Novi kod možete tražiti {EmailCodeService.ResendInterval.TotalSeconds:0} sekundi poslije prethodnog.";
    public const string BlockedByAdmin = "Karticu je blokirao administrator. Obratite se podršci.";
    public const string CloseNeedsZeroBalance = "Kartica s balansom većim od 0 ne može se zatvoriti.";
    public const string Conflict = "Kartica je u međuvremenu promijenjena. Osvježite podatke i pokušajte ponovo.";

    private static readonly string[] OpenStatuses = [CardStatusCodes.Pending, CardStatusCodes.Active, CardStatusCodes.Blocked];

    // Iz ovih statusa se ne izlazi, pa kartica u njima ne može ostati podrazumijevana za primanje novca
    private static readonly string[] FinalStatuses = [CardStatusCodes.Lost, CardStatusCodes.Stolen, CardStatusCodes.Closed];

    public Task<PagedResult<CardResponse>> GetMineAsync(CardSearchObject search, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        return Newest(Filter(context.Cards.AsNoTracking().Where(c => c.UserId == userId), search))
            .ToPagedResultAsync<Card, CardResponse>(search, cancellationToken);
    }

    public async Task<CardResponse> GetMineByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        return await context.Cards.AsNoTracking()
            .Where(c => c.Id == id && c.UserId == userId)
            .ProjectToType<CardResponse>()
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(NotFound);
    }

    public async Task<CardResponse> CreateAsync(CardInsertRequest request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        await EnsureReferencesExistAsync(request, cancellationToken);
        var (network, last4) = ReadNumber(request.CardNumber);
        var expiresAt = ReadExpiry(request.Expiry);

        var openCards = await context.Cards
            .CountAsync(c => c.UserId == userId && OpenStatuses.Contains(c.CardStatus.Code), cancellationToken);
        if (openCards >= MaxOpenCards)
        {
            throw new BusinessException(TooManyCards);
        }

        // Pun broj se ne čuva, pa se duplikat prepoznaje po mreži, zadnje 4 cifre i isteku
        if (await context.Cards.AnyAsync(c => c.UserId == userId && c.Network == network && c.Last4Digits == last4
                && c.ExpiresAt == expiresAt && OpenStatuses.Contains(c.CardStatus.Code), cancellationToken))
        {
            throw new BusinessException($"Kartica •••• {last4} je već dodana.");
        }

        var user = await context.Users.SingleAsync(u => u.Id == userId, cancellationToken);
        var card = new Card
        {
            User = user,
            CardTypeId = request.CardTypeId,
            CurrencyId = request.CurrencyId,
            CardStatus = await StatusAsync(CardStatusCodes.Pending, cancellationToken),
            Name = string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim(),
            Network = network,
            Last4Digits = last4,
            ExpiresAt = expiresAt,
        };
        context.Cards.Add(card);
        var code = await codes.CreateForCardAsync(card, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        // Mail se traži tek kad je kartica snimljena. Kartica tada već postoji, pa greška reda ne smije javiti da
        // dodavanje nije uspjelo (ponovni pokušaj bi dobio "već dodana"). Korisnik traži novi kod kroz resend-code
        await PublishCodeAsync(user, card, code, cancellationToken);
        logger.LogInformation("Korisnik {UserId} je dodao karticu {CardId}, čeka aktivaciju.", userId, card.Id);
        return await GetResponseAsync(card.Id, cancellationToken);
    }

    public async Task<CardResponse> VerifyAsync(int id, CardVerifyRequest request, CancellationToken cancellationToken = default)
    {
        var card = await LoadOwnAsync(id, cancellationToken);
        CardStateMachine.EnsureCanChange(card.CardStatus.Code, CardStatusCodes.Active, CardActor.Owner);

        var token = await codes.FindValidForCardAsync(card.Id, request.Code, cancellationToken)
            ?? throw new BusinessException(AuthService.InvalidCode);

        var oldCode = await ApplyStatusAsync(card, CardStatusCodes.Active, CardActor.Owner,
            "Kartica je aktivirana kodom iz emaila.", cancellationToken);

        // Prva aktivna kartica prima novac kad se plaća korisniku, a ne kartici
        if (!await context.Cards.AnyAsync(c => c.UserId == card.UserId && c.IsDefault, cancellationToken))
        {
            card.IsDefault = true;
        }

        // UseAsync troši kod i u istom snimanju snima i aktivaciju kartice
        await SaveAsync(async () =>
        {
            if (!await codes.UseAsync(token, cancellationToken))
            {
                throw new BusinessException(AuthService.InvalidCode);
            }
        });

        await PublishStatusChangedAsync(card, oldCode, null, cancellationToken);
        return await GetResponseAsync(card.Id, cancellationToken);
    }

    public async Task ResendCodeAsync(int id, CancellationToken cancellationToken = default)
    {
        var card = await LoadOwnAsync(id, cancellationToken);
        if (card.CardStatus.Code != CardStatusCodes.Pending)
        {
            throw new BusinessException(CodeOnlyForPending);
        }

        if (!await codes.CanSendAsync(card.UserId, EmailTokenPurpose.CardActivation, cancellationToken))
        {
            throw new BusinessException(ResendTooSoon);
        }

        var user = await context.Users.SingleAsync(u => u.Id == card.UserId, cancellationToken);
        var code = await codes.CreateForCardAsync(card, cancellationToken);
        await SaveAsync(() => context.SaveChangesAsync(cancellationToken));
        await PublishCodeAsync(user, card, code, cancellationToken);
        logger.LogInformation("Korisnik {UserId} je dobio novi kod za karticu {CardId}.", user.Id, card.Id);
    }

    public async Task<CardResponse> BlockAsync(int id, CancellationToken cancellationToken = default)
    {
        var card = await LoadOwnAsync(id, cancellationToken);
        return await ChangeAndSaveAsync(card, CardStatusCodes.Blocked, CardActor.Owner,
            "Korisnik je blokirao karticu.", null, cancellationToken);
    }

    public async Task<CardResponse> UnblockAsync(int id, CancellationToken cancellationToken = default)
    {
        var card = await LoadOwnAsync(id, cancellationToken);
        CardStateMachine.EnsureCanChange(card.CardStatus.Code, CardStatusCodes.Active, CardActor.Owner);
        await EnsureNotBlockedByAdminAsync(card.Id, cancellationToken);
        return await ChangeAndSaveAsync(card, CardStatusCodes.Active, CardActor.Owner,
            "Korisnik je deblokirao karticu.", null, cancellationToken);
    }

    public async Task<CardResponse> ReportAsync(int id, CardReportRequest request, CancellationToken cancellationToken = default)
    {
        var card = await LoadOwnAsync(id, cancellationToken);
        var stolen = request.Reason == CardReportReason.Stolen;
        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        var description = note
            ?? (stolen ? "Korisnik je prijavio karticu kao ukradenu." : "Korisnik je prijavio karticu kao izgubljenu.");
        return await ChangeAndSaveAsync(card, stolen ? CardStatusCodes.Stolen : CardStatusCodes.Lost, CardActor.Owner,
            description, note, cancellationToken);
    }

    public Task<PagedResult<CardResponse>> GetAllAsync(AdminCardSearchObject search, CancellationToken cancellationToken = default)
    {
        var query = Filter(context.Cards.AsNoTracking(), search);
        if (search.UserId is not null)
        {
            query = query.Where(c => c.UserId == search.UserId);
        }

        if (!string.IsNullOrWhiteSpace(search.Fts))
        {
            var fts = search.Fts.Trim();
            query = query.Where(c => (c.Name != null && c.Name.Contains(fts)) || c.Last4Digits.Contains(fts)
                || c.User.FirstName.Contains(fts) || c.User.LastName.Contains(fts) || c.User.Username.Contains(fts));
        }

        return Newest(query).ToPagedResultAsync<Card, CardResponse>(search, cancellationToken);
    }

    public async Task<CardResponse> ChangeStatusAsync(int id, CardStatusChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        var card = await context.Cards.Include(c => c.CardStatus).FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException(NotFound);
        var target = await context.CardStatuses.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.CardStatusId, cancellationToken)
            ?? throw FieldError("cardStatusId", StatusMissing);

        CardStateMachine.EnsureCanChange(card.CardStatus.Code, target.Code, CardActor.Admin);
        if (target.Code == CardStatusCodes.Closed && card.Balance > 0)
        {
            throw new BusinessException(CloseNeedsZeroBalance);
        }

        // Dužina se provjerava i poslije Trim, da razmaci ne prođu kao razlog u audit tragu
        var reason = (request.Reason ?? "").Trim();
        if (reason.Length < CardStatusChangeRequest.ReasonMinLength)
        {
            throw FieldError("reason", CardStatusChangeRequest.ReasonLengthMessage);
        }

        return await ChangeAndSaveAsync(card, target.Code, CardActor.Admin, reason, reason, cancellationToken);
    }

    // Korisnik vidi samo svoje kartice. Tuđa kartica daje isti odgovor kao nepostojeća, da se ne otkrije da postoji
    private async Task<Card> LoadOwnAsync(int id, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        return await context.Cards.Include(c => c.CardStatus)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(NotFound);
    }

    private async Task<CardResponse> ChangeAndSaveAsync(Card card, string toCode, CardActor actor, string description,
        string? reason, CancellationToken cancellationToken)
    {
        var oldCode = await ApplyStatusAsync(card, toCode, actor, description, cancellationToken);
        await SaveAsync(() => context.SaveChangesAsync(cancellationToken));
        await PublishStatusChangedAsync(card, oldCode, reason, cancellationToken);
        return await GetResponseAsync(card.Id, cancellationToken);
    }

    // Jedino mjesto koje mijenja status kartice. Ne snima, da pozivalac snimi prelaz zajedno sa svojim izmjenama
    private async Task<string> ApplyStatusAsync(Card card, string toCode, CardActor actor, string description,
        CancellationToken cancellationToken)
    {
        var oldCode = card.CardStatus.Code;
        CardStateMachine.EnsureCanChange(oldCode, toCode, actor);

        card.CardStatus = await StatusAsync(toCode, cancellationToken);
        if (FinalStatuses.Contains(toCode))
        {
            card.IsDefault = false;
        }

        // Blokiranje ima svoju akciju, jer se po njoj zna ko je karticu blokirao (vidi EnsureNotBlockedByAdminAsync)
        context.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUser.UserId,
            Action = toCode == CardStatusCodes.Blocked ? AuditActions.CardBlocked : AuditActions.CardStatusChanged,
            EntityType = nameof(Card),
            EntityId = card.Id,
            OldValues = $$"""{"Status":"{{oldCode}}"}""",
            NewValues = $$"""{"Status":"{{toCode}}"}""",
            Description = description,
        });
        return oldCode;
    }

    // Dva istovremena zahtjeva nad istom karticom: drugi naiđe na RowVersion, a dvije istovremene aktivacije na
    // jedinstvenu podrazumijevanu karticu. Oba slučaja dobijaju poruku umjesto greške servera
    private static async Task SaveAsync(Func<Task> save)
    {
        try
        {
            await save();
        }
        catch (DbUpdateException)
        {
            throw new BusinessException(Conflict);
        }
    }

    private async Task PublishCodeAsync(User user, Card card, string code, CancellationToken cancellationToken)
    {
        try
        {
            await publisher.PublishAsync(QueueNames.Email, AuthEmails.CardActivation(user, card, code), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Kod za karticu {CardId} nije objavljen na red.", card.Id);
        }
    }

    // Prelaz je već snimljen, pa greška reda ne smije korisniku javiti da prelaz nije uspio. Ostaje u logu
    private async Task PublishStatusChangedAsync(Card card, string oldCode, string? reason, CancellationToken cancellationToken)
    {
        var message = new CardStatusChanged(Guid.NewGuid(), card.Id, oldCode, card.CardStatus.Code, currentUser.UserId, reason);
        try
        {
            await publisher.PublishAsync(QueueNames.CardStatus, message, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Promjena statusa kartice {CardId} nije objavljena na red.", card.Id);
        }

        logger.LogInformation("Kartica {CardId}: {OldStatus} → {NewStatus}, promijenio korisnik {UserId}.",
            card.Id, oldCode, card.CardStatus.Code, currentUser.UserId);
    }

    // Korisnik ne može sam poništiti blokadu koju je postavio administrator. Odlučuje posljednje blokiranje
    private async Task EnsureNotBlockedByAdminAsync(int cardId, CancellationToken cancellationToken)
    {
        var blockedByRole = await context.AuditLogs.AsNoTracking()
            .Where(a => a.EntityType == nameof(Card) && a.EntityId == cardId && a.Action == AuditActions.CardBlocked)
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Select(a => a.ActorUser != null ? a.ActorUser.Role.Name : null)
            .FirstOrDefaultAsync(cancellationToken);

        if (blockedByRole == AppRoles.Admin)
        {
            throw new BusinessException(BlockedByAdmin);
        }
    }

    private Task<CardStatus> StatusAsync(string code, CancellationToken cancellationToken) =>
        context.CardStatuses.SingleAsync(s => s.Code == code, cancellationToken);

    private async Task<CardResponse> GetResponseAsync(int id, CancellationToken cancellationToken) =>
        await context.Cards.AsNoTracking().Where(c => c.Id == id).ProjectToType<CardResponse>().SingleAsync(cancellationToken);

    private static IQueryable<Card> Filter(IQueryable<Card> query, CardSearchObject search)
    {
        if (search.CardStatusId is not null)
        {
            query = query.Where(c => c.CardStatusId == search.CardStatusId);
        }

        if (search.CardTypeId is not null)
        {
            query = query.Where(c => c.CardTypeId == search.CardTypeId);
        }

        // Admin pretraga ima i vlasnika, pa je njena pretraga posebno u GetAllAsync
        if (search is not AdminCardSearchObject && !string.IsNullOrWhiteSpace(search.Fts))
        {
            var fts = search.Fts.Trim();
            query = query.Where(c => (c.Name != null && c.Name.Contains(fts)) || c.Last4Digits.Contains(fts));
        }

        return query;
    }

    // Najnovija kartica je prva, pa se tek dodana kartica vidi na vrhu liste
    private static IOrderedQueryable<Card> Newest(IQueryable<Card> query) =>
        query.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id);

    private async Task EnsureReferencesExistAsync(CardInsertRequest request, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (!await context.CardTypes.AnyAsync(t => t.Id == request.CardTypeId, cancellationToken))
        {
            errors["cardTypeId"] = [CardTypeMissing];
        }

        if (!await context.Currencies.AnyAsync(c => c.Id == request.CurrencyId, cancellationToken))
        {
            errors["currencyId"] = [CurrencyMissing];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    // Provjera se ponavlja i ovdje, jer servis mogu zvati i testovi i drugi servisi bez validacije modela.
    // Iz broja se uzimaju samo mreža i zadnje 4 cifre, a sam broj se dalje ne prosljeđuje
    private static (CardNetwork Network, string Last4) ReadNumber(string number)
    {
        if (number.Length != CardNumber.Length || !CardNumber.IsValidLuhn(number))
        {
            throw FieldError("cardNumber", CardNumberAttribute.InvalidNumber);
        }

        var network = CardNumber.DetectNetwork(number) ?? throw FieldError("cardNumber", CardNumberAttribute.UnsupportedNetwork);
        return (network, number[^4..]);
    }

    // MM/GG znači da kartica važi do kraja tog mjeseca
    private static DateOnly ReadExpiry(string expiry)
    {
        if (expiry.Length != 5
            || !int.TryParse(expiry[..2], NumberStyles.None, CultureInfo.InvariantCulture, out var month)
            || !int.TryParse(expiry[3..], NumberStyles.None, CultureInfo.InvariantCulture, out var year)
            || month is < 1 or > 12)
        {
            throw FieldError("expiry", "Datum isteka je u formatu MM/GG (npr. 08/29).");
        }

        year += 2000;
        var expiresAt = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (expiresAt < today)
        {
            throw FieldError("expiry", CardExpired);
        }

        if (expiresAt > today.AddYears(MaxExpiryYears))
        {
            throw FieldError("expiry", ExpiryTooFar);
        }

        return expiresAt;
    }

    private static ValidationException FieldError(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
