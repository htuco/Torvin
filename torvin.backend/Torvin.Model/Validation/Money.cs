namespace Torvin.Model.Validation;

// Pravilo iznosa koji korisnik unosi, na jednom mjestu. Iznos s više od dvije decimale se odbija, a ne zaokružuje,
// da korisnik pošalje tačno ono što je vidio na ekranu
public static class Money
{
    public const decimal MinAmount = 0.01m;
    public const decimal MaxAmount = 100_000m;
    public const int MaxDecimals = 2;

    public const string InvalidAmount = "Iznos mora biti između 0,01 i 100.000,00, s najviše dvije decimale.";

    public static bool IsValidAmount(decimal amount) =>
        amount is >= MinAmount and <= MaxAmount && decimal.Round(amount, MaxDecimals) == amount;
}
