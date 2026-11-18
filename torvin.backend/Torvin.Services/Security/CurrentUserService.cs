using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Torvin.Model.Exceptions;

namespace Torvin.Services.Security;

public interface ICurrentUserService
{
    int UserId { get; }
    string Role { get; }
    string TokenId { get; }
    DateTime TokenExpiresAt { get; }
}

// Prijavljeni korisnik iz claimova koje je JwtBearer već provjerio. Servisi ID korisnika uzimaju samo odavde
public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    public int UserId => int.Parse(Claim(TorvinClaims.UserId), CultureInfo.InvariantCulture);

    public string Role => Claim(TorvinClaims.Role);

    public string TokenId => Claim(TorvinClaims.TokenId);

    public DateTime TokenExpiresAt =>
        DateTimeOffset.FromUnixTimeSeconds(long.Parse(Claim(TorvinClaims.ExpiresAt), CultureInfo.InvariantCulture)).UtcDateTime;

    private string Claim(string type)
    {
        var user = accessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedException();
        }

        return user.FindFirstValue(type) ?? throw new UnauthorizedException();
    }
}
