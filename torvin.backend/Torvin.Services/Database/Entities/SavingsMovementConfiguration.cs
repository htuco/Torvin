using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Torvin.Model.Enums;

namespace Torvin.Services.Database.Entities;

public class SavingsMovementConfiguration : IEntityTypeConfiguration<SavingsMovement>
{
    public void Configure(EntityTypeBuilder<SavingsMovement> builder)
    {
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_SavingsMovements_Amount", "[Amount] > 0");
            t.HasCheckConstraint("CK_SavingsMovements_Type", EnumCheckConstraint.Sql<SavingsMovementType>(nameof(SavingsMovement.Type)));
        });

        // Cilj se briše samo mekano, a pokreti ostaju kao historija novca i skrivaju se zajedno s ciljem
        builder.HasQueryFilter(m => !m.SavingsGoal.IsDeleted);

        builder.Property(m => m.Amount).HasPrecision(19, 4);

        builder.HasOne(m => m.SavingsGoal).WithMany(g => g.Movements).HasForeignKey(m => m.SavingsGoalId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.Card).WithMany().HasForeignKey(m => m.CardId).OnDelete(DeleteBehavior.Restrict);
    }
}
