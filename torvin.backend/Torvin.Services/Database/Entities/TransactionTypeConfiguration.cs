using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Torvin.Model.Constants;

namespace Torvin.Services.Database.Entities;

public class TransactionTypeConfiguration : IEntityTypeConfiguration<TransactionType>
{
    public void Configure(EntityTypeBuilder<TransactionType> builder)
    {
        builder.Property(t => t.Code).IsRequired().HasMaxLength(30);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(50);
        builder.HasIndex(t => t.Code).IsUnique();

        builder.HasData(
            Create(1, TransactionTypeCodes.Transfer, "Interni transfer"),
            Create(2, TransactionTypeCodes.Correction, "Korektivna transakcija"),
            Create(3, TransactionTypeCodes.StandingOrder, "Trajni nalog"),
            Create(4, TransactionTypeCodes.MoneyRequest, "Plaćen zahtjev za novac"));
    }

    private static TransactionType Create(int id, string code, string name) => new()
    {
        Id = id, Code = code, Name = name,
        CreatedAt = ReferenceSeed.CreatedAt, ModifiedAt = ReferenceSeed.CreatedAt,
    };
}
