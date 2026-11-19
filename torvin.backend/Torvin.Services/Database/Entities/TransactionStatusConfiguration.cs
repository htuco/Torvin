using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Torvin.Model.Constants;

namespace Torvin.Services.Database.Entities;

public class TransactionStatusConfiguration : IEntityTypeConfiguration<TransactionStatus>
{
    public void Configure(EntityTypeBuilder<TransactionStatus> builder)
    {
        builder.Property(s => s.Code).IsRequired().HasMaxLength(30);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(50);
        builder.HasIndex(s => s.Code).IsUnique();

        builder.HasData(
            Create(1, TransactionStatusCodes.Completed, "Izvršena"),
            Create(2, TransactionStatusCodes.Failed, "Neuspješna"),
            Create(3, TransactionStatusCodes.Reversed, "Poništena"));
    }

    private static TransactionStatus Create(int id, string code, string name) => new()
    {
        Id = id, Code = code, Name = name,
        CreatedAt = ReferenceSeed.CreatedAt, ModifiedAt = ReferenceSeed.CreatedAt,
    };
}
