using System.Globalization;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Torvin.Model.Enums;
using Torvin.Services.Database;
using Torvin.Services.Database.Entities;

namespace Torvin.Services.Security;

public interface IEmailCodeService
{
    // Dodaje novi kod u kontekst i vraća ga u čistom obliku za mail. Snima pozivalac, zajedno sa svojim izmjenama
    Task<string> CreateAsync(User user, EmailTokenPurpose purpose, CancellationToken cancellationToken = default);

    Task<bool> CanSendAsync(int userId, EmailTokenPurpose purpose, CancellationToken cancellationToken = default);

    // Kod za aktivaciju kartice je vezan za tu karticu, pa novi kod poništava samo stari kod iste kartice
    Task<string> CreateForCardAsync(Card card, CancellationToken cancellationToken = default);

    // Vraća važeći kod kad je unesen tačno, a null za svaki neuspjeh. Kod se ovdje ne troši
    Task<EmailToken?> FindValidAsync(string email, string code, EmailTokenPurpose purpose,
        CancellationToken cancellationToken = default);

    // Isto kao FindValidAsync, ali kod mora pripadati baš ovoj kartici
    Task<EmailToken?> FindValidForCardAsync(int cardId, string code, CancellationToken cancellationToken = default);

    // Troši kod i u istom SaveChanges snima izmjene koje je pozivalac već napravio (npr. nova lozinka).
    // Vraća false kad je drugi zahtjev u međuvremenu iskoristio isti kod
    Task<bool> UseAsync(EmailToken token, CancellationToken cancellationToken = default);
}

// Šestocifreni kodovi za potvrdu emaila i reset lozinke. Kod ima samo milion mogućnosti, pa bi brzi hash
// (SHA-256) iz procurjele baze bio razbijen za milisekunde. Zato se hashira kao lozinka (PBKDF2 sa solju)
public sealed class EmailCodeService(TorvinDbContext context) : IEmailCodeService
{
    private static readonly Lazy<string> DummyCodeHash = new(() => PasswordHasher.Hash("000000"));

    public const int MaxAttempts = 5;
    public static readonly TimeSpan ResendInterval = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan RegistrationLifetime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan PasswordResetLifetime = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan CardActivationLifetime = TimeSpan.FromMinutes(15);

    // Svaki konflikt znači da je drugi zahtjev uspio upisati, pa petlja uvijek napreduje
    private const int MaxConcurrencyRetries = 20;

    public static TimeSpan LifetimeOf(EmailTokenPurpose purpose) => purpose switch
    {
        EmailTokenPurpose.Registration => RegistrationLifetime,
        EmailTokenPurpose.PasswordReset => PasswordResetLifetime,
        EmailTokenPurpose.CardActivation => CardActivationLifetime,
        _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, "Svrha koda nema zadan rok važenja."),
    };

    public async Task<string> CreateAsync(User user, EmailTokenPurpose purpose, CancellationToken cancellationToken = default)
    {
        // Novi kod poništava stare iste svrhe, pa važi samo onaj iz posljednjeg maila
        if (user.Id != 0)
        {
            await ExpireAsync(context.EmailTokens.Where(t => t.UserId == user.Id && t.Purpose == purpose), cancellationToken);
        }

        return Add(new EmailToken { User = user, Purpose = purpose });
    }

    public async Task<string> CreateForCardAsync(Card card, CancellationToken cancellationToken = default)
    {
        if (card.Id != 0)
        {
            await ExpireAsync(context.EmailTokens.Where(t => t.CardId == card.Id && t.Purpose == EmailTokenPurpose.CardActivation),
                cancellationToken);
        }

        return Add(new EmailToken { UserId = card.UserId, Card = card, Purpose = EmailTokenPurpose.CardActivation });
    }

    public async Task<bool> CanSendAsync(int userId, EmailTokenPurpose purpose, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow - ResendInterval;
        return !await context.EmailTokens.AsNoTracking()
            .AnyAsync(t => t.UserId == userId && t.Purpose == purpose && t.CreatedAt > since, cancellationToken);
    }

    public async Task<EmailToken?> FindValidAsync(string email, string code, EmailTokenPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;
        var token = await context.EmailTokens
            .Include(t => t.User)
            .Where(t => t.User.Email == normalizedEmail && t.Purpose == purpose && t.UsedAt == null && t.ExpiresAt > now)
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return await VerifyAsync(token, code, cancellationToken);
    }

    public async Task<EmailToken?> FindValidForCardAsync(int cardId, string code, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var token = await context.EmailTokens
            .Where(t => t.CardId == cardId && t.Purpose == EmailTokenPurpose.CardActivation && t.UsedAt == null && t.ExpiresAt > now)
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return await VerifyAsync(token, code, cancellationToken);
    }

    public async Task<bool> UseAsync(EmailToken token, CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            token.UsedAt = DateTime.UtcNow;
            try
            {
                await context.SaveChangesAsync(cancellationToken);
                return true;
            }
            // Konflikt na kodu: ili ga je drugi zahtjev iskoristio, ili je samo povećao brojač pokušaja.
            // Kod se pročita ponovo, a ostale izmjene pozivaoca ostaju i snimaju se u sljedećem pokušaju
            catch (DbUpdateConcurrencyException ex) when (attempt < MaxConcurrencyRetries
                && ex.Entries.All(e => e.Entity == token))
            {
                await context.Entry(token).ReloadAsync(cancellationToken);
                if (token.UsedAt is not null)
                {
                    return false;
                }
            }
        }
    }

    private async Task ExpireAsync(IQueryable<EmailToken> tokens, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var previous = await tokens.Where(t => t.UsedAt == null && t.ExpiresAt > now).ToListAsync(cancellationToken);
        foreach (var token in previous)
        {
            token.ExpiresAt = now;
        }
    }

    private string Add(EmailToken token)
    {
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        token.CodeHash = PasswordHasher.Hash(code);
        token.ExpiresAt = DateTime.UtcNow + LifetimeOf(token.Purpose);
        context.EmailTokens.Add(token);
        return code;
    }

    private async Task<EmailToken?> VerifyAsync(EmailToken? token, string code, CancellationToken cancellationToken)
    {
        if (token is null || !await CountAttemptAsync(token, cancellationToken))
        {
            // Provjera traje isto i bez važećeg koda, da trajanje ne otkrije postoji li aktivan kod
            PasswordHasher.Verify(code, DummyCodeHash.Value);
            return null;
        }

        return PasswordHasher.Verify(code.Trim(), token.CodeHash) ? token : null;
    }

    // Pokušaj se snima prije poređenja (i kad operacija poslije padne), da paralelni zahtjevi ne dobiju više
    // od 5 poređenja. Vraća false kad je kod u međuvremenu iskorišten, istekao ili su pokušaji potrošeni
    private async Task<bool> CountAttemptAsync(EmailToken token, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            if (token.UsedAt is not null || token.ExpiresAt <= DateTime.UtcNow || token.AttemptCount >= MaxAttempts)
            {
                return false;
            }

            token.AttemptCount++;
            try
            {
                await context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyRetries)
            {
                await context.Entry(token).ReloadAsync(cancellationToken);
            }
        }
    }
}
