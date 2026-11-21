using System.Buffers.Text;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Torvin.Services.Configuration;

namespace Torvin.Services.Security;

public sealed record ReceiveCodePayload(int CardId, decimal? Amount, DateTime ExpiresAt);

public interface IReceiveCodeSigner
{
    string Create(ReceiveCodePayload payload);

    ReceiveCodePayload? Read(string code);
}

// Kod za QR: "torvin:v1:<payload>.<potpis>", oba dijela u base64url. Payload je čitljiv, ali ga bez ključa iz
// RECEIVE_CODE_KEY niko ne može promijeniti, a sam ID kartice ništa ne otvara. Rok provjerava ReceiveCodeService
public sealed class ReceiveCodeSigner(IOptions<ReceiveCodeOptions> options) : IReceiveCodeSigner
{
    public const string Prefix = "torvin:v1:";

    private const char PartSeparator = '.';
    private const char FieldSeparator = '|';

    public string Create(ReceiveCodePayload payload)
    {
        var amount = payload.Amount?.ToString(CultureInfo.InvariantCulture) ?? "";
        var expiresAt = new DateTimeOffset(payload.ExpiresAt, TimeSpan.Zero).ToUnixTimeSeconds();
        var data = Encoding.UTF8.GetBytes(
            string.Create(CultureInfo.InvariantCulture, $"{payload.CardId}{FieldSeparator}{amount}{FieldSeparator}{expiresAt}"));

        return Prefix + Base64Url.EncodeToString(data) + PartSeparator + Base64Url.EncodeToString(Sign(data));
    }

    // Svaki neispravan oblik daje null, bez razlike zašto, da odgovor ne pomaže u pogađanju
    public ReceiveCodePayload? Read(string code)
    {
        if (string.IsNullOrEmpty(code) || !code.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return null;
        }

        var parts = code[Prefix.Length..].Split(PartSeparator);
        if (parts.Length != 2 || !Base64Url.IsValid(parts[0]) || !Base64Url.IsValid(parts[1]))
        {
            return null;
        }

        var data = Base64Url.DecodeFromChars(parts[0]);
        if (!CryptographicOperations.FixedTimeEquals(Sign(data), Base64Url.DecodeFromChars(parts[1])))
        {
            return null;
        }

        return Parse(Encoding.UTF8.GetString(data));
    }

    private byte[] Sign(byte[] data) => HMACSHA256.HashData(Encoding.UTF8.GetBytes(options.Value.Key), data);

    private static ReceiveCodePayload? Parse(string text)
    {
        var fields = text.Split(FieldSeparator);
        if (fields.Length != 3
            || !int.TryParse(fields[0], NumberStyles.None, CultureInfo.InvariantCulture, out var cardId)
            || !long.TryParse(fields[2], NumberStyles.None, CultureInfo.InvariantCulture, out var expiresAt))
        {
            return null;
        }

        decimal? amount = null;
        if (fields[1].Length > 0)
        {
            if (!decimal.TryParse(fields[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
            {
                return null;
            }

            amount = value;
        }

        return new ReceiveCodePayload(cardId, amount, DateTimeOffset.FromUnixTimeSeconds(expiresAt).UtcDateTime);
    }
}
