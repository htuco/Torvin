using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Torvin.Services.Security;

namespace Torvin.Tests;

public class JwtTokenServiceTests
{
    private static TokenValidationParameters Parameters(string key, string audience, string? issuer = null) => new()
    {
        IssuerSigningKey = JwtTokenService.SigningKey(key),
        ValidIssuer = issuer ?? AuthServiceTests.Jwt.Issuer,
        ValidAudience = audience,
    };

    [Fact]
    public async Task IssuedToken_HasSubjectRoleTokenIdAndExpiry()
    {
        var issued = new JwtTokenService(Options.Create(AuthServiceTests.Jwt)).Issue(42, "User");

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(issued.Token,
            Parameters(AuthServiceTests.Jwt.Key, AuthServiceTests.Jwt.Audience));

        Assert.True(result.IsValid);
        Assert.Equal("42", result.Claims[TorvinClaims.UserId]);
        Assert.Equal("User", result.Claims[TorvinClaims.Role]);
        Assert.Equal(issued.TokenId, result.Claims[TorvinClaims.TokenId]);
        Assert.InRange(issued.ExpiresAt, DateTime.UtcNow.AddMinutes(59), DateTime.UtcNow.AddMinutes(61));
    }

    [Theory]
    [InlineData("drugi-kljuc-drugi-kljuc-drugi-kljuc", "torvin-test", "torvin-test")]
    [InlineData("0123456789abcdef0123456789abcdef", "druga-aplikacija", "torvin-test")]
    [InlineData("0123456789abcdef0123456789abcdef", "torvin-test", "drugi-izdavac")]
    public async Task WrongKeyAudienceOrIssuer_FailsValidation(string key, string audience, string issuer)
    {
        var issued = new JwtTokenService(Options.Create(AuthServiceTests.Jwt)).Issue(42, "User");

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(issued.Token, Parameters(key, audience, issuer));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void EveryToken_HasDifferentTokenId()
    {
        var service = new JwtTokenService(Options.Create(AuthServiceTests.Jwt));

        Assert.NotEqual(service.Issue(1, "User").TokenId, service.Issue(1, "User").TokenId);
    }
}
