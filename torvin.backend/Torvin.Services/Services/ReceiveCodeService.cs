using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Torvin.Model.Constants;
using Torvin.Model.Exceptions;
using Torvin.Model.Requests;
using Torvin.Model.Responses;
using Torvin.Model.Validation;
using Torvin.Services.Database;
using Torvin.Services.Security;

namespace Torvin.Services.Services;

public interface IReceiveCodeService
{
    Task<ReceiveCodeResponse> CreateAsync(int cardId, decimal? amount, CancellationToken cancellationToken = default);

    Task<ReceiveCodeRecipientResponse> ResolveAsync(ReceiveCodeResolveRequest request,
        CancellationToken cancellationToken = default);
}

// QR kod za primanje novca: vlasnik ga pravi za svoju aktivnu karticu, a pošiljalac ga razriješi u prikaz primaoca.
// Kod nosi samo podatke koje potpis štiti, a stanje kartice i korisnika se pri razrješavanju ponovo čita iz baze
public sealed class ReceiveCodeService(
    TorvinDbContext context,
    ICurrentUserService currentUser,
    IReceiveCodeSigner signer,
    ILogger<ReceiveCodeService> logger) : IReceiveCodeService
{
    // Kod na ekranu se lako fotografiše i proslijedi, pa kratko važi. Mobilna aplikacija traži novi kad istekne
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);

    public const string CardNotActive = "Kod za primanje se pravi samo za aktivnu karticu.";
    public const string CodeInvalid = "QR kod nije ispravan.";
    public const string CodeExpired = "QR kod je istekao. Zatražite novi od primaoca.";

    public async Task<ReceiveCodeResponse> CreateAsync(int cardId, decimal? amount,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (amount is not null && !Money.IsValidAmount(amount.Value))
        {
            throw FieldError("amount", Money.InvalidAmount);
        }

        var statusCode = await context.Cards.AsNoTracking()
            .Where(c => c.Id == cardId && c.UserId == userId)
            .Select(c => c.CardStatus.Code)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(CardService.NotFound);
        if (statusCode != CardStatusCodes.Active)
        {
            throw new BusinessException(CardNotActive);
        }

        var expiresAt = DateTime.UtcNow.Add(Lifetime);
        var code = signer.Create(new ReceiveCodePayload(cardId, amount, expiresAt));
        logger.LogInformation("Korisnik {UserId} je napravio kod za primanje za karticu {CardId}.", userId, cardId);
        return new ReceiveCodeResponse(code, amount, expiresAt);
    }

    // Kartica i primalac se ponovo provjeravaju, jer su se mogli promijeniti poslije izdavanja koda. Blokirana
    // kartica i blokiran račun daju istu poruku kao kod prenosa, da se ne otkriva stanje tuđeg računa
    public async Task<ReceiveCodeRecipientResponse> ResolveAsync(ReceiveCodeResolveRequest request,
        CancellationToken cancellationToken = default)
    {
        var payload = signer.Read(request.Code.Trim()) ?? throw FieldError("code", CodeInvalid);
        if (payload.ExpiresAt <= DateTime.UtcNow)
        {
            throw FieldError("code", CodeExpired);
        }

        var recipient = await context.Users.AsNoTracking()
            .Where(RecipientService.CanReceiveMoney)
            .SelectMany(u => u.Cards)
            .Where(c => c.Id == payload.CardId && c.CardStatus.Code == CardStatusCodes.Active)
            .Select(c => new ReceiveCodeRecipientResponse(c.UserId, c.User.FirstName, c.User.LastName, c.User.Username,
                c.Network, c.Last4Digits, c.Currency.Code, payload.Amount))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw FieldError("code", TransactionService.RecipientNotFound);
        if (recipient.UserId == currentUser.UserId)
        {
            throw FieldError("code", TransactionService.CannotSendToSelf);
        }

        return recipient;
    }

    private static ValidationException FieldError(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
