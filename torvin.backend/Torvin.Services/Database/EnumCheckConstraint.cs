namespace Torvin.Services.Database;

// Enum se u bazi čuva kao broj, pa ograničenje ne dozvoljava broj koji nije vrijednost enuma.
// Raspon se računa iz samog enuma (vrijednosti idu redom od 1), pa nova vrijednost ne traži ručnu izmjenu
public static class EnumCheckConstraint
{
    public static string Sql<TEnum>(string column) where TEnum : struct, Enum
    {
        var values = Enum.GetValues<TEnum>().Select(value => Convert.ToInt32(value)).ToList();
        return $"[{column}] BETWEEN {values.Min()} AND {values.Max()}";
    }
}
