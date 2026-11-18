using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Torvin.Services.Database;
using Torvin.Services.Database.Entities;

namespace Torvin.Services.Security;

public interface ITokenRevocationService
{
    Task<bool> IsRevokedAsync(string tokenId, CancellationToken cancellationToken = default);

    Task RevokeAsync(string tokenId, int userId, DateTime expiresAt, CancellationToken cancellationToken = default);
}

// Provjera ide na svaki request, pa se odgovor drži u kešu. "Opozvan" i "provjeren, nije opozvan" su
// odvojeni ključevi: zahtjev koji je pročitao bazu prije odjave ne može prepisati oznaku opoziva
public sealed class TokenRevocationService(TorvinDbContext context, IMemoryCache cache) : ITokenRevocationService
{
    private static readonly TimeSpan NotRevokedCacheDuration = TimeSpan.FromMinutes(1);

    public async Task<bool> IsRevokedAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(RevokedKey(tokenId), out _))
        {
            return true;
        }

        if (cache.TryGetValue(NotRevokedKey(tokenId), out _))
        {
            return false;
        }

        var revoked = await context.RevokedTokens.AsNoTracking()
            .Where(t => t.Jti == tokenId)
            .Select(t => (DateTime?)t.ExpiresAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (revoked is not null)
        {
            cache.Set(RevokedKey(tokenId), true, new DateTimeOffset(revoked.Value));
            return true;
        }

        cache.Set(NotRevokedKey(tokenId), true, NotRevokedCacheDuration);
        return cache.TryGetValue(RevokedKey(tokenId), out _);
    }

    public async Task RevokeAsync(string tokenId, int userId, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        cache.Set(RevokedKey(tokenId), true, new DateTimeOffset(expiresAt));
        if (await context.RevokedTokens.AsNoTracking().AnyAsync(t => t.Jti == tokenId, cancellationToken))
        {
            return;
        }

        var revokedToken = new RevokedToken { Jti = tokenId, UserId = userId, ExpiresAt = expiresAt };
        context.RevokedTokens.Add(revokedToken);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Dvije istovremene odjave istim tokenom: druga naiđe na unique indeks, a token je već opozvan
            context.Entry(revokedToken).State = EntityState.Detached;
            if (!await context.RevokedTokens.AsNoTracking().AnyAsync(t => t.Jti == tokenId, cancellationToken))
            {
                throw;
            }
        }
    }

    private static string RevokedKey(string tokenId) => $"revoked-token:{tokenId}";

    private static string NotRevokedKey(string tokenId) => $"not-revoked-token:{tokenId}";
}
