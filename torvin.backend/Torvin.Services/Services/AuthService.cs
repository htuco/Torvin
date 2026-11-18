using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Torvin.Model.Constants;
using Torvin.Model.Enums;
using Torvin.Model.Exceptions;
using Torvin.Model.Requests;
using Torvin.Model.Responses;
using Torvin.Services.Database;
using Torvin.Services.Database.Entities;
using Torvin.Services.Security;

namespace Torvin.Services.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task LogoutAsync(CancellationToken cancellationToken = default);
}

public sealed class AuthService(
    TorvinDbContext context,
    IJwtTokenService tokens,
    ITokenRevocationService revocation,
    ICurrentUserService currentUser,
    ILogger<AuthService> logger) : IAuthService
{
    public const int MaxFailedLogins = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    // Ista poruka za nepostojeći račun i pogrešnu lozinku, da odgovor ne otkriva koji računi postoje
    public const string InvalidCredentials = "Pogrešno korisničko ime ili lozinka.";

    // Lozinka se provjerava i za nepostojeći račun, da trajanje odgovora ne otkrije koji računi postoje
    private static readonly Lazy<string> DummyPasswordHash = new(() => PasswordHasher.Hash(Guid.NewGuid().ToString()));

    // Svaki konflikt znači da je drugi zahtjev uspio upisati, pa petlja uvijek napreduje
    private const int MaxConcurrencyRetries = 20;

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        // Korisnička imena i emailovi se čuvaju malim slovima
        var login = request.Login.Trim().ToLowerInvariant();
        var user = await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == login || u.Email == login, cancellationToken);

        if (user is null)
        {
            PasswordHasher.Verify(request.Password, DummyPasswordHash.Value);
            throw new BusinessException(InvalidCredentials);
        }

        var passwordHash = await context.UserPasswords
            .AsNoTracking()
            .Where(p => p.UserId == user.Id)
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Select(p => p.PasswordHash)
            .FirstOrDefaultAsync(cancellationToken);
        var passwordIsValid = passwordHash is not null && PasswordHasher.Verify(request.Password, passwordHash);

        // Zaključan račun s pogrešnom lozinkom daje istu poruku kao nepostojeći, pa se zaključavanje
        // ne može iskoristiti za provjeru da li račun postoji
        var now = DateTime.UtcNow;
        if (user.LockoutEndAt > now)
        {
            if (!passwordIsValid)
            {
                throw new BusinessException(InvalidCredentials);
            }

            var minutes = (int)Math.Ceiling((user.LockoutEndAt.Value - now).TotalMinutes);
            throw new ForbiddenException(
                $"Račun je privremeno zaključan zbog previše pogrešnih pokušaja. Pokušajte ponovo za {minutes} min.");
        }

        if (!passwordIsValid)
        {
            await RegisterFailedLoginAsync(user, now, cancellationToken);
            throw new BusinessException(InvalidCredentials);
        }

        if (user.FailedLoginCount != 0 || user.LockoutEndAt is not null)
        {
            await UpdateLoginStateAsync(user, u =>
            {
                u.FailedLoginCount = 0;
                u.LockoutEndAt = null;
            }, cancellationToken);
        }

        // Ove provjere idu tek poslije lozinke, da se stanje računa ne otkriva bez ispravne lozinke
        if (!user.IsActive)
        {
            throw new ForbiddenException("Račun je blokiran. Obratite se podršci.");
        }

        if (!user.EmailConfirmed)
        {
            throw new ForbiddenException("Email nije potvrđen.");
        }

        EnsureAppMatchesRole(request.App!.Value, user.Role.Name);

        var token = tokens.Issue(user.Id, user.Role.Name);
        logger.LogInformation("Korisnik {UserId} se prijavio u aplikaciju {App}.", user.Id, request.App);
        return new LoginResponse(token.Token, token.ExpiresAt, user.Role.Name, user.FirstName, user.LastName);
    }

    public Task LogoutAsync(CancellationToken cancellationToken = default) =>
        revocation.RevokeAsync(currentUser.TokenId, currentUser.UserId, currentUser.TokenExpiresAt, cancellationToken);

    private async Task RegisterFailedLoginAsync(User user, DateTime now, CancellationToken cancellationToken)
    {
        await UpdateLoginStateAsync(user, u =>
        {
            u.FailedLoginCount++;
            if (u.FailedLoginCount >= MaxFailedLogins)
            {
                u.FailedLoginCount = 0;
                u.LockoutEndAt = now + LockoutDuration;
            }
        }, cancellationToken);

        if (user.LockoutEndAt > now)
        {
            logger.LogWarning("Račun {UserId} je zaključan poslije {Attempts} pogrešnih lozinki.", user.Id, MaxFailedLogins);
        }
    }

    // Brojač je concurrency token: kad dva zahtjeva istovremeno mijenjaju stanje prijave, drugi upis padne,
    // stanje se pročita ponovo i izmjena primijeni na svježe vrijednosti, pa se nijedan pokušaj ne izgubi
    private async Task UpdateLoginStateAsync(User user, Action<User> apply, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            apply(user);
            try
            {
                await context.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyRetries)
            {
                await context.Entry(user).ReloadAsync(cancellationToken);
            }
        }
    }

    // Desktop je samo za administratore, a mobilna samo za korisnike (prijava teme §4)
    private static void EnsureAppMatchesRole(ClientApp app, string role)
    {
        if (app == ClientApp.Desktop && role != AppRoles.Admin)
        {
            throw new ForbiddenException("Desktop aplikacija je namijenjena samo administratorima.");
        }

        if (app == ClientApp.Mobile && role == AppRoles.Admin)
        {
            throw new ForbiddenException("Mobilna aplikacija je namijenjena korisnicima. Administratori koriste desktop aplikaciju.");
        }
    }
}
