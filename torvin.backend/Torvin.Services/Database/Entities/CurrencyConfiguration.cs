using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Torvin.Model.Constants;

namespace Torvin.Services.Database.Entities;

public class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.Property(c => c.Code).IsRequired().HasMaxLength(3).IsFixedLength();
        builder.Property(c => c.Name).IsRequired().HasMaxLength(50);
        builder.Property(c => c.Symbol).IsRequired().HasMaxLength(5);
        builder.HasIndex(c => c.Code).IsUnique();

        builder.HasData(
            Create(1, CurrencyCodes.BAM, "Konvertibilna marka", "KM"),
            Create(2, CurrencyCodes.EUR, "Euro", "€"),
            Create(3, CurrencyCodes.USD, "Američki dolar", "$"));
    }

    private static Currency Create(int id, string code, string name, string symbol) => new()
    {
        Id = id, Code = code, Name = name, Symbol = symbol,
        CreatedAt = ReferenceSeed.CreatedAt, ModifiedAt = ReferenceSeed.CreatedAt,
    };
}
