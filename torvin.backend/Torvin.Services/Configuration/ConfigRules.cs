namespace Torvin.Services.Configuration;

internal static class ConfigRules
{
    public const string NotSet = " nije zadan.";

    public const int MinPort = 1;
    public const int MaxPort = 65535;

    // HS256 traži ključ od najmanje 256 bita
    public const int JwtKeyMinLength = 32;
    public const int JwtMinExpiresMinutes = 1;
    public const int JwtMaxExpiresMinutes = 1440;
}
