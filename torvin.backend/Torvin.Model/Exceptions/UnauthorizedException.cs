namespace Torvin.Model.Exceptions;

// Nema prijave ili je istekla: klijent tada vodi korisnika na ekran za prijavu
public sealed class UnauthorizedException() : Exception(DefaultMessage)
{
    public const string DefaultMessage = "Prijava je istekla ili nije ispravna. Prijavite se ponovo.";
}
