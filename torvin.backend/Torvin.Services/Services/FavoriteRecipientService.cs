using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Torvin.Model.Exceptions;
using Torvin.Model.Requests;
using Torvin.Model.Responses;
using Torvin.Model.SearchObjects;
using Torvin.Services.Database;
using Torvin.Services.Database.Entities;
using Torvin.Services.Security;

namespace Torvin.Services.Services;

public interface IFavoriteRecipientService
{
    Task<PagedResult<FavoriteRecipientResponse>> GetMineAsync(FavoriteRecipientSearchObject search,
        CancellationToken cancellationToken = default);

    Task<FavoriteRecipientResponse> CreateAsync(FavoriteRecipientInsertRequest request,
        CancellationToken cancellationToken = default);

    Task<FavoriteRecipientResponse> UpdateAsync(int id, FavoriteRecipientUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

// Omiljeni primaoci prijavljenog korisnika. Vlasnik se uzima iz tokena, a tuđi zapis daje 404. Brisanje je meko
// (SoftDeleteInterceptor), pa isti primalac kasnije može ponovo postati omiljeni
public sealed class FavoriteRecipientService(
    TorvinDbContext context,
    ICurrentUserService currentUser,
    ILogger<FavoriteRecipientService> logger) : IFavoriteRecipientService
{
    public const string NotFound = "Omiljeni primalac nije pronađen.";
    public const string AlreadyFavorite = "Primalac je već među omiljenim.";

    private static readonly Expression<Func<FavoriteRecipient, FavoriteRecipientResponse>> ToResponse = f =>
        new FavoriteRecipientResponse(f.Id, f.RecipientUserId, f.RecipientUser.FirstName, f.RecipientUser.LastName,
            f.RecipientUser.Username, f.Nickname, f.CreatedAt);

    public Task<PagedResult<FavoriteRecipientResponse>> GetMineAsync(FavoriteRecipientSearchObject search,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        var query = context.FavoriteRecipients.AsNoTracking().Where(f => f.UserId == userId);

        var fts = search.Fts?.Trim();
        if (!string.IsNullOrEmpty(fts))
        {
            query = query.Where(f => (f.Nickname != null && f.Nickname.Contains(fts))
                || f.RecipientUser.FirstName.Contains(fts) || f.RecipientUser.LastName.Contains(fts)
                || f.RecipientUser.Username.Contains(fts));
        }

        return query
            .OrderBy(f => f.RecipientUser.LastName)
            .ThenBy(f => f.RecipientUser.FirstName)
            .ThenBy(f => f.Id)
            .Select(ToResponse)
            .ToPagedResultAsync(search, cancellationToken);
    }

    public async Task<FavoriteRecipientResponse> CreateAsync(FavoriteRecipientInsertRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (request.RecipientUserId == userId)
        {
            throw FieldError("recipientUserId", TransactionService.CannotSendToSelf);
        }

        if (!await context.Users.Where(u => u.Id == request.RecipientUserId).AnyAsync(RecipientService.CanReceiveMoney,
                cancellationToken))
        {
            throw FieldError("recipientUserId", TransactionService.RecipientNotFound);
        }

        if (await context.FavoriteRecipients.AnyAsync(f => f.UserId == userId && f.RecipientUserId == request.RecipientUserId,
                cancellationToken))
        {
            throw new BusinessException(AlreadyFavorite);
        }

        var favorite = new FavoriteRecipient
        {
            UserId = userId,
            RecipientUserId = request.RecipientUserId,
            Nickname = Normalize(request.Nickname),
        };
        context.FavoriteRecipients.Add(favorite);

        // Dva istovremena dodavanja istog primaoca: oba prođu provjeru, a drugo zaustavi unique indeks
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new BusinessException(AlreadyFavorite);
        }

        logger.LogInformation("Korisnik {UserId} je dodao omiljenog primaoca {RecipientUserId}.", userId, request.RecipientUserId);
        return await GetResponseAsync(favorite.Id, cancellationToken);
    }

    public async Task<FavoriteRecipientResponse> UpdateAsync(int id, FavoriteRecipientUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var favorite = await LoadOwnAsync(id, cancellationToken);
        favorite.Nickname = Normalize(request.Nickname);
        await context.SaveChangesAsync(cancellationToken);
        return await GetResponseAsync(favorite.Id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var favorite = await LoadOwnAsync(id, cancellationToken);
        context.FavoriteRecipients.Remove(favorite);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Korisnik {UserId} je uklonio omiljenog primaoca {FavoriteId}.", favorite.UserId, id);
    }

    private async Task<FavoriteRecipient> LoadOwnAsync(int id, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        return await context.FavoriteRecipients.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(NotFound);
    }

    private Task<FavoriteRecipientResponse> GetResponseAsync(int id, CancellationToken cancellationToken) =>
        context.FavoriteRecipients.AsNoTracking()
            .Where(f => f.Id == id)
            .Select(ToResponse)
            .SingleAsync(cancellationToken);

    private static string? Normalize(string? nickname) => string.IsNullOrWhiteSpace(nickname) ? null : nickname.Trim();

    private static ValidationException FieldError(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
