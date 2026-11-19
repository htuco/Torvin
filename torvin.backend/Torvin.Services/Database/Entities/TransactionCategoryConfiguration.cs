using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Torvin.Services.Database.Entities;

public class TransactionCategoryConfiguration : IEntityTypeConfiguration<TransactionCategory>
{
    public void Configure(EntityTypeBuilder<TransactionCategory> builder)
    {
        builder.Property(c => c.Name).IsRequired().HasMaxLength(50);
        builder.Property(c => c.Description).HasMaxLength(200);
        builder.HasIndex(c => c.Name).IsUnique();

        builder.HasData(
            Create(1, "Hrana", "Namirnice, restorani i dostava."),
            Create(2, "Prijevoz", "Gorivo, javni prijevoz i taksi."),
            Create(3, "Režije", "Struja, voda, grijanje, internet i telefon."),
            Create(4, "Zabava", "Kino, koncerti, pretplate i izlasci."),
            Create(5, "Zdravlje", "Apoteka, pregledi i teretana."),
            Create(6, "Kupovina", "Odjeća, tehnika i ostala kupovina."),
            Create(7, "Ostalo", "Sve što ne spada u druge kategorije."));
    }

    private static TransactionCategory Create(int id, string name, string description) => new()
    {
        Id = id, Name = name, Description = description,
        CreatedAt = ReferenceSeed.CreatedAt, ModifiedAt = ReferenceSeed.CreatedAt,
    };
}
