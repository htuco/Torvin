using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Torvin.Model.Enums;

namespace Torvin.Services.Database.Entities;

public class StandingOrderExecutionConfiguration : IEntityTypeConfiguration<StandingOrderExecution>
{
    public void Configure(EntityTypeBuilder<StandingOrderExecution> builder)
    {
        builder.ToTable(t => t.HasCheckConstraint("CK_StandingOrderExecutions_Status", EnumCheckConstraint.Sql<StandingOrderExecutionStatus>(nameof(StandingOrderExecution.Status))));

        builder.Property(e => e.FailureReason).HasMaxLength(300);

        // Jedna transakcija je rezultat najviše jednog izvršenja
        builder.HasIndex(e => e.TransactionId).IsUnique().HasFilter("[TransactionId] IS NOT NULL");

        // Nalog se ne briše nego otkazuje, pa historija pokušaja uvijek ostaje
        builder.HasOne(e => e.StandingOrder).WithMany(o => o.Executions).HasForeignKey(e => e.StandingOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Transaction).WithMany().HasForeignKey(e => e.TransactionId).OnDelete(DeleteBehavior.Restrict);
    }
}
