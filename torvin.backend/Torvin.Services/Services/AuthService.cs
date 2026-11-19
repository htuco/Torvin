using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Torvin.Model.Constants;
using Torvin.Model.Enums;
using Torvin.Model.Exceptions;
using Torvin.Model.Messages;
using Torvin.Model.Requests;
using Torvin.Model.Responses;
using Torvin.Services.Database;
using Torvin.Services.Database.Entities;
using Torvin.Services.Messaging;
using Torvin.Services.Security;

namespace Torvin.Services.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task LogoutAsync(CancellationToken cancellationToken = default);

    Task<MessageResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default);

    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}

public sealed class AuthService(
    TorvinDbContext context,
    IJwtTokenService tokens,
    ITokenRevocationService revocation,
    ICurrentUserService currentUser,
    IEmailCodeService codes,
    IMessagePublisher publisher,
    ILogger<AuthService> logger) : IAuthService
{
    public const int MaxFailedLogins = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    // Koliko posljednjih lozinki reset ne smije ponoviti
    public const int PasswordHistoryDepth = 3;

    // Ista poruka za nepostojeći račun i pogrešnu lozinku, da odgovor ne otkriva koji računi postoje
    public const string InvalidCredentials = "Pogrešno korisničko ime ili lozinka.";

    // Ista poruka za nepostojeći email, pogrešan, istekao i iskorišten kod
    public const string InvalidCode = "Kod nije ispravan ili je istekao. Zatražite novi kod.";

    public const string EmailNotConfirmed = "Email nije potvrđen. Kod za potvrdu je poslan na vaš email.";
    public const string UsernameTaken = "Korisničko ime je zauzeto.";
    public const string EmailTaken = "Email je već registrovan.";
    public const string PasswordContainsIdentity = "Lozinka ne smije sadržati korisničko ime ni email.";
    public const string PasswordRecentlyUsed = "Nova lozinka ne smije biti jedna od zadnje tri.";
    public const string Registered = "Registracija je uspjela. Poslali smo kod za potvrdu na vaš email.";

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

        // Novi kod ide kroz prijavu, pa nema posebnog anonimnog endpointa za ponovno slanje
        if (!user.EmailConfirmed)
        {
            if (await codes.CanSendAsync(user.Id, EmailTokenPurpose.Registration, cancellationToken))
            {
                var code = await codes.CreateAsync(user, EmailTokenPurpose.Registration, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
                await publisher.PublishAsync(QueueNames.Email, AuthEmails.Registration(user, code), cancellationToken);
                logger.LogInformation("Nepotvrđen korisnik {UserId} je dobio novi kod pri prijavi.", user.Id);
            }

            throw new ForbiddenException(EmailNotConfirmed);
        }

        EnsureAppMatchesRole(request.App!.Value, user.Role.Name);

        var token = tokens.Issue(user.Id, user.Role.Name);
        logger.LogInformation("Korisnik {UserId} se prijavio u aplikaciju {App}.", user.Id, request.App);
        return new LoginResponse(token.Token, token.ExpiresAt, user.Role.Name, user.FirstName, user.LastName);
    }

    public Task LogoutAsync(CancellationToken cancellationToken = default) =>
        revocation.RevokeAsync(currentUser.TokenId, currentUser.UserId, currentUser.TokenExpiresAt, cancellationToken);

    public async Task<MessageResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        // Korisnička imena i emailovi se čuvaju malim slovima, kao što ih prijava i traži
        var username = Normalize(request.Username);
        var email = Normalize(request.Email);
        await EnsureAvailableAsync(username, email, cancellationToken);
        EnsurePasswordHasNoIdentity(request.Password, username, email, "password");

        // Rola se nikad ne čita iz requesta: svaka registracija je običan korisnik (upute §5)
        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Username = username,
            Email = email,
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            Role = await context.Roles.SingleAsync(r => r.Name == AppRoles.User, cancellationToken),
            EmailConfirmed = false,
        };
        context.Users.Add(user);
        context.UserPasswords.Add(new UserPassword { User = user, PasswordHash = PasswordHasher.Hash(request.Password) });
        var code = await codes.CreateAsync(user, EmailTokenPurpose.Registration, cancellationToken);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Dvije istovremene registracije istog imena ili emaila: druga naiđe na unique indeks
            context.ChangeTracker.Clear();
            await EnsureAvailableAsync(username, email, cancellationToken);
            throw;
        }

        // Mail se traži tek kad je korisnik snimljen, da Worker ne šalje kod za račun koji ne postoji
        await publisher.PublishAsync(QueueNames.Email, AuthEmails.Registration(user, code), cancellationToken);
        logger.LogInformation("Registrovan je korisnik {UserId}.", user.Id);
        return new MessageResponse(Registered);
    }

    public async Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default)
    {
        var token = await codes.FindValidAsync(request.Email, request.Code, EmailTokenPurpose.Registration, cancellationToken)
            ?? throw new BusinessException(InvalidCode);

        token.User.EmailConfirmed = true;
        if (!await codes.UseAsync(token, cancellationToken))
        {
            throw new BusinessException(InvalidCode);
        }

        logger.LogInformation("Korisnik {UserId} je potvrdio email.", token.UserId);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        // Odgovor je isti za postojeći i nepostojeći email, pa se ovdje ništa ne baca
        var email = Normalize(request.Email);
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return;
        }

        if (!await codes.CanSendAsync(user.Id, EmailTokenPurpose.PasswordReset, cancellationToken))
        {
            logger.LogInformation("Korisnik {UserId} je tražio novi kod za reset prije isteka pauze.", user.Id);
            return;
        }

        var code = await codes.CreateAsync(user, EmailTokenPurpose.PasswordReset, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        try
        {
            await publisher.PublishAsync(QueueNames.Email, AuthEmails.PasswordReset(user, code), cancellationToken);
        }
        // Greška bi dala 500 samo za postojeći račun i tako otkrila koji email postoji. Korisnik poslije pauze
        // može tražiti novi kod, a greška ostaje u logu
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Kod za reset korisnika {UserId} nije objavljen na red.", user.Id);
            return;
        }

        logger.LogInformation("Korisnik {UserId} je dobio kod za reset lozinke.", user.Id);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var token = await codes.FindValidAsync(request.Email, request.Code, EmailTokenPurpose.PasswordReset, cancellationToken)
            ?? throw new BusinessException(InvalidCode);
        var user = token.User;

        // Provjere lozinke idu tek uz ispravan kod, da odgovor ne otkrije korisničko ime vlasnika emaila.
        // Odbijena lozinka ne troši kod, pa korisnik može odmah pokušati s drugom
        EnsurePasswordHasNoIdentity(request.NewPassword, user.Username, user.Email, "newPassword");
        var recentHashes = await context.UserPasswords.AsNoTracking()
            .Where(p => p.UserId == user.Id)
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Take(PasswordHistoryDepth)
            .Select(p => p.PasswordHash)
            .ToListAsync(cancellationToken);
        if (recentHashes.Any(hash => PasswordHasher.Verify(request.NewPassword, hash)))
        {
            throw FieldError("newPassword", PasswordRecentlyUsed);
        }

        context.UserPasswords.Add(new UserPassword { UserId = user.Id, PasswordHash = PasswordHasher.Hash(request.NewPassword) });
        user.FailedLoginCount = 0;
        user.LockoutEndAt = null;
        // Kod je stigao na ovaj email, pa je i vlasništvo emaila dokazano
        user.EmailConfirmed = true;

        if (!await codes.UseAsync(token, cancellationToken))
        {
            throw new BusinessException(InvalidCode);
        }

        logger.LogInformation("Korisnik {UserId} je promijenio lozinku preko koda za reset.", user.Id);
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    // Sve zauzete vrijednosti se vraćaju odjednom, da korisnik ne ispravlja formu dvaput
    private async Task EnsureAvailableAsync(string username, string email, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (await context.Users.AnyAsync(u => u.Username == username, cancellationToken))
        {
            errors["username"] = [UsernameTaken];
        }

        if (await context.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            errors["email"] = [EmailTaken];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    // Lozinka koja sadrži korisničko ime ili dio emaila prije @ se lako pogađa
    private static void EnsurePasswordHasNoIdentity(string password, string username, string email, string field)
    {
        var lowered = password.ToLowerInvariant();
        var localPart = email.Split('@')[0];
        if (lowered.Contains(username) || (localPart.Length >= 3 && lowered.Contains(localPart)))
        {
            throw FieldError(field, PasswordContainsIdentity);
        }
    }

    private static ValidationException FieldError(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });

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
