using Torvin.Services.Security;

namespace Torvin.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void CorrectPassword_Verifies()
    {
        var hash = PasswordHasher.Hash("Lozinka.123");

        Assert.True(PasswordHasher.Verify("Lozinka.123", hash));
        Assert.StartsWith("PBKDF2-SHA256$600000$", hash);
    }

    [Fact]
    public void WrongPassword_DoesNotVerify()
    {
        var hash = PasswordHasher.Hash("Lozinka.123");

        Assert.False(PasswordHasher.Verify("lozinka.123", hash));
    }

    [Fact]
    public void SamePassword_GetsDifferentHashes()
    {
        Assert.NotEqual(PasswordHasher.Hash("test"), PasswordHasher.Hash("test"));
    }

    [Fact]
    public void UnknownFormat_DoesNotVerify()
    {
        Assert.False(PasswordHasher.Verify("test", "md5$abc"));
    }

    [Theory]
    [InlineData("PBKDF2-SHA256$600000$nije-base64!$AAAA")]
    [InlineData("PBKDF2-SHA256$0$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAA==")]
    [InlineData("PBKDF2-SHA256$600000$$")]
    public void CorruptedHash_DoesNotVerify(string storedHash)
    {
        Assert.False(PasswordHasher.Verify("test", storedHash));
    }
}
