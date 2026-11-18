using Microsoft.IdentityModel.JsonWebTokens;

namespace Torvin.Services.Security;

// Imena claimova u JWT-u na jednom mjestu: izdavanje tokena, validacija i čitanje korisnika ih dijele
public static class TorvinClaims
{
    public const string UserId = JwtRegisteredClaimNames.Sub;
    public const string TokenId = JwtRegisteredClaimNames.Jti;
    public const string ExpiresAt = JwtRegisteredClaimNames.Exp;
    public const string Role = "role";
}
