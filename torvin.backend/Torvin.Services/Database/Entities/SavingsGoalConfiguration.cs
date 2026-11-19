using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Torvin.Services.Database.Entities;

public class SavingsGoalConfiguration : IEntityTypeConfiguration<SavingsGoal>
{
    public void Configure(EntityTypeBuilder<SavingsGoal> builder)
    {
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_SavingsGoals_TargetAmount", "[TargetAmount] > 0");
            t.HasCheckConstraint("CK_SavingsGoals_CurrentAmount", "[CurrentAmount] >= 0");
        });
        builder.HasQueryFilter(g => !g.IsDeleted);

        builder.Property(g => g.Name).IsRequired().HasMaxLength(100);
        builder.Property(g => g.TargetAmount).HasPrecision(19, 4);
        builder.Property(g => g.CurrentAmount).HasPrecision(19, 4);

        builder.HasOne(g => g.User).WithMany().HasForeignKey(g => g.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(g => g.Currency).WithMany().HasForeignKey(g => g.CurrencyId).OnDelete(DeleteBehavior.Restrict);
    }
}
