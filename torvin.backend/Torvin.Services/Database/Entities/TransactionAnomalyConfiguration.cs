using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Torvin.Services.Database.Entities;

public class TransactionAnomalyConfiguration : IEntityTypeConfiguration<TransactionAnomaly>
{
    public void Configure(EntityTypeBuilder<TransactionAnomaly> builder)
    {
        builder.Property(a => a.Score).HasPrecision(5, 2);
        builder.Property(a => a.AmountScore).HasPrecision(5, 2);
        builder.Property(a => a.FrequencyScore).HasPrecision(5, 2);
        builder.Property(a => a.TimeScore).HasPrecision(5, 2);
        builder.Property(a => a.RecipientScore).HasPrecision(5, 2);

        builder.HasOne(a => a.Transaction).WithOne().HasForeignKey<TransactionAnomaly>(a => a.TransactionId).OnDelete(DeleteBehavior.Restrict);
    }
}
