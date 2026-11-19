using System.Security.Cryptography;

namespace Torvin.Services.Security;

// PBKDF2 sa SHA-256 i nasumičnom solju po lozinci. U hashu su zapisani algoritam, broj iteracija i so,
// pa se stari hashevi mogu provjeriti i kad se broj iteracija kasnije poveća
public static class PasswordHasher
{
    private const string Algorithm = "PBKDF2-SHA256";
    private const int Iterations = 600_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Algorithm}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split('$');
        if (parts.Length != 4 || parts[0] != Algorithm || !int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        // Oštećen zapis u bazi znači "lozinka nije ispravna", a ne grešku servera
        var salt = new byte[parts[2].Length];
        var expected = new byte[parts[3].Length];
        if (iterations <= 0
            || !Convert.TryFromBase64String(parts[2], salt, out var saltLength)
            || !Convert.TryFromBase64String(parts[3], expected, out var hashLength)
            || saltLength == 0 || hashLength == 0)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt.AsSpan(0, saltLength), iterations, HashAlgorithmName.SHA256, hashLength);

        // Poređenje u konstantnom vremenu, da trajanje ne otkrije koliko se bajtova poklapa
        return CryptographicOperations.FixedTimeEquals(actual, expected.AsSpan(0, hashLength));
    }
}
