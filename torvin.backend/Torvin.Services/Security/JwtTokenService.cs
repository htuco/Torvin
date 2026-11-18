using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Torvin.Services.Configuration;

namespace Torvin.Services.Security;

public sealed record IssuedToken(string Token, string TokenId, DateTime ExpiresAt);

public interface IJwtTokenService
{
    IssuedToken Issue(int userId, string role);
}

// Token potpisan ključem iz JWT_KEY. Jti je jedinstven po tokenu, da se pri odjavi može opozvati baš taj token
public sealed class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    public IssuedToken Issue(int userId, string role)
    {
        var jwt = options.Value;
        var now = DateTime.UtcNow;
        var tokenId = Guid.NewGuid().ToString("N");
        var expiresAt = now.AddMinutes(jwt.ExpiresMinutes);

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            Claims = new Dictionary<string, object>
            {
                [TorvinClaims.UserId] = userId.ToString(CultureInfo.InvariantCulture),
                [TorvinClaims.Role] = role,
                [TorvinClaims.TokenId] = tokenId,
            },
            SigningCredentials = new SigningCredentials(SigningKey(jwt.Key), SecurityAlgorithms.HmacSha256),
        });

        return new IssuedToken(token, tokenId, expiresAt);
    }

    public static SymmetricSecurityKey SigningKey(string key) => new(Encoding.UTF8.GetBytes(key));
}
