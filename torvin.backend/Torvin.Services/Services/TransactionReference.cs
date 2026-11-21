using System.Globalization;
using System.Security.Cryptography;

namespace Torvin.Services.Services;

// Broj reference koji korisnik vidi umjesto ID-a: TRV + datum + 6 slučajnih cifara (npr. TRV20261121004817)
public static class TransactionReference
{
    public static string Format(DateTime at, int number) =>
        string.Create(CultureInfo.InvariantCulture, $"TRV{at:yyyyMMdd}{number:D6}");

    // Kolizija u istom danu je moguća, ali rijetka, a unique indeks je odbija
    public static string New(DateTime at) => Format(at, RandomNumberGenerator.GetInt32(1_000_000));
}
