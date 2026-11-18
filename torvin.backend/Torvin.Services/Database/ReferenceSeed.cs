namespace Torvin.Services.Database;

// Fiksan datum za HasData, da svaka nova migracija ne vidi lažnu promjenu seeda
public static class ReferenceSeed
{
    public static readonly DateTime CreatedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}
