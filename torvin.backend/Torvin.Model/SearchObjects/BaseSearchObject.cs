using System.ComponentModel.DataAnnotations;

namespace Torvin.Model.SearchObjects;

// Svaka lista je paginirana, a veličina stranice ima gornju granicu (upute §8.2)
public class BaseSearchObject
{
    public const int MaxPageSize = 100;

    // Granica stranice čuva Skip od prekoračenja (stranica × veličina mora stati u int)
    public const int MaxPage = 100_000;

    [Range(1, MaxPage, ErrorMessage = "Stranica mora biti između 1 i 100.000.")]
    public int Page { get; set; } = 1;

    [Range(1, MaxPageSize, ErrorMessage = "Veličina stranice mora biti između 1 i 100.")]
    public int PageSize { get; set; } = 20;

    [MaxLength(100, ErrorMessage = "Pretraga može imati najviše 100 znakova.")]
    public string? Fts { get; set; }
}
