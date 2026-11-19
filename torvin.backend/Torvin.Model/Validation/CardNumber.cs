using Torvin.Model.Enums;

namespace Torvin.Model.Validation;

// Provjera broja kartice pri dodavanju. Broj se samo provjerava, a čuvaju se zadnje 4 cifre i mreža
public static class CardNumber
{
    public const int Length = 16;

    // Luhn: svaka druga cifra zdesna se udvostruči (od dvocifrenog rezultata se oduzme 9), a zbir mora biti djeljiv s 10
    public static bool IsValidLuhn(string number)
    {
        if (number.Length == 0 || !number.All(char.IsAsciiDigit))
        {
            return false;
        }

        var sum = 0;
        for (var i = 0; i < number.Length; i++)
        {
            var digit = number[number.Length - 1 - i] - '0';
            if (i % 2 == 1)
            {
                digit *= 2;
                if (digit > 9)
                {
                    digit -= 9;
                }
            }

            sum += digit;
        }

        return sum % 10 == 0;
    }

    // Visa počinje s 4, a Mastercard s 51–55 ili 2221–2720. Ostale mreže nisu podržane
    public static CardNetwork? DetectNetwork(string number)
    {
        if (number.Length < 4 || !number.All(char.IsAsciiDigit))
        {
            return null;
        }

        if (number[0] == '4')
        {
            return CardNetwork.Visa;
        }

        var firstTwo = int.Parse(number[..2]);
        var firstFour = int.Parse(number[..4]);
        if (firstTwo is >= 51 and <= 55 || firstFour is >= 2221 and <= 2720)
        {
            return CardNetwork.Mastercard;
        }

        return null;
    }
}
