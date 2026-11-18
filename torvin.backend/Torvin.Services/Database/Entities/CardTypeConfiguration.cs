using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Torvin.Services.Database.Entities;

public class CardTypeConfiguration : IEntityTypeConfiguration<CardType>
{
    public void Configure(EntityTypeBuilder<CardType> builder)
    {
        builder.Property(t => t.Name).IsRequired().HasMaxLength(50);
        builder.Property(t => t.Description).HasMaxLength(200);
        builder.HasIndex(t => t.Name).IsUnique();

        builder.HasData(
            Create(1, "Debitna", "Kartica za svakodnevno plaćanje i transfere."),
            Create(2, "Prepaid", "Kartica s unaprijed uplaćenim iznosom."),
            Create(3, "Poslovna", "Kartica za poslovne troškove."));
    }

    private static CardType Create(int id, string name, string description) => new()
    {
        Id = id, Name = name, Description = description,
        CreatedAt = ReferenceSeed.CreatedAt, ModifiedAt = ReferenceSeed.CreatedAt,
    };
}
