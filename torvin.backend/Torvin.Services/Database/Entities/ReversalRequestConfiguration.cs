using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Torvin.Model.Enums;

namespace Torvin.Services.Database.Entities;

public class ReversalRequestConfiguration : IEntityTypeConfiguration<ReversalRequest>
{
    public void Configure(EntityTypeBuilder<ReversalRequest> builder)
    {
        builder.ToTable(t => t.HasCheckConstraint("CK_ReversalRequests_Status", EnumCheckConstraint.Sql<ReversalRequestStatus>(nameof(ReversalRequest.Status))));

        builder.Property(r => r.Reason).IsRequired().HasMaxLength(500);
        builder.Property(r => r.DecisionReason).HasMaxLength(500);

        // Korektivna transakcija pripada tačno jednom odobrenom zahtjevu
        builder.HasIndex(r => r.CorrectiveTransactionId).IsUnique().HasFilter("[CorrectiveTransactionId] IS NOT NULL");

        builder.HasOne(r => r.Transaction).WithMany().HasForeignKey(r => r.TransactionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.RequestedByUser).WithMany().HasForeignKey(r => r.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.DecidedByUser).WithMany().HasForeignKey(r => r.DecidedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.CorrectiveTransaction).WithMany().HasForeignKey(r => r.CorrectiveTransactionId).OnDelete(DeleteBehavior.Restrict);
    }
}
