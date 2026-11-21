using System.Buffers.Text;
using System.Text;
using Microsoft.Extensions.Options;
using Torvin.Services.Configuration;
using Torvin.Services.Security;

namespace Torvin.Tests;

public class ReceiveCodeSignerTests
{
    public const string Key = "fedcba9876543210fedcba9876543210";

    private static readonly DateTime ExpiresAt = new(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public static ReceiveCodeSigner NewSigner(string key = Key) => new(Options.Create(new ReceiveCodeOptions { Key = key }));

    // Payload zamijenjen tekstom po izboru, a potpis ostaje stari (napadač nema ključ)
    private static string WithPayload(string code, string payload)
    {
        var signature = code.Split('.')[1];
        return ReceiveCodeSigner.Prefix + Base64Url.EncodeToString(Encoding.UTF8.GetBytes(payload)) + "." + signature;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("25.50")]
    [InlineData("100000")]
    public void Create_ThenRead_ReturnsSamePayload(string? amountText)
    {
        decimal? amount = amountText is null ? null : decimal.Parse(amountText, System.Globalization.CultureInfo.InvariantCulture);
        var signer = NewSigner();

        var code = signer.Create(new ReceiveCodePayload(42, amount, ExpiresAt));

        Assert.StartsWith(ReceiveCodeSigner.Prefix, code);
        Assert.Equal(new ReceiveCodePayload(42, amount, ExpiresAt), signer.Read(code));
    }

    [Theory]
    [InlineData("43|25.50|1893499200")]
    [InlineData("42|2550|1893499200")]
    [InlineData("42||1893499200")]
    [InlineData("42|25.50|1993499200")]
    public void Read_TamperedPayload_ReturnsNull(string payload)
    {
        var signer = NewSigner();
        var code = signer.Create(new ReceiveCodePayload(42, 25.50m, ExpiresAt));

        Assert.Null(signer.Read(WithPayload(code, payload)));
    }

    [Fact]
    public void Read_UnchangedPayloadText_IsAccepted()
    {
        var signer = NewSigner();
        var code = signer.Create(new ReceiveCodePayload(42, 25.50m, ExpiresAt));

        // Provjera da WithPayload pravi isti oblik: nepromijenjen tekst prolazi, pa gornji test hvata baš izmjenu
        Assert.NotNull(signer.Read(WithPayload(code, "42|25.50|1893499200")));
    }

    [Fact]
    public void Read_TamperedSignature_ReturnsNull()
    {
        var signer = NewSigner();
        var code = signer.Create(new ReceiveCodePayload(42, null, ExpiresAt));
        var last = code[^1];

        Assert.Null(signer.Read(code[..^1] + (last == 'A' ? 'B' : 'A')));
    }

    [Fact]
    public void Read_CodeSignedWithOtherKey_ReturnsNull()
    {
        var code = NewSigner("0123456789abcdef0123456789abcdef").Create(new ReceiveCodePayload(42, null, ExpiresAt));

        Assert.Null(NewSigner().Read(code));
    }

    [Theory]
    [InlineData("")]
    [InlineData("torvin:v1:")]
    [InlineData("torvin:v2:abc.def")]
    [InlineData("torvin:v1:abc")]
    [InlineData("torvin:v1:a.b.c")]
    [InlineData("torvin:v1:!!!.???")]
    [InlineData("https://torvin.ba/pay/42")]
    public void Read_MalformedCode_ReturnsNull(string code)
    {
        Assert.Null(NewSigner().Read(code));
    }
}
