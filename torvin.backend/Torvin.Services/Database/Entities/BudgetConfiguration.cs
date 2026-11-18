using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Torvin.Services.Database.Entities;

public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Budgets_LimitAmount", "[LimitAmount] > 0");
            t.HasCheckConstraint("CK_Budgets_Month", "[Month] BETWEEN 1 AND 12");
        });
        builder.HasQueryFilter(b => !b.IsDeleted);

        builder.Property(b => b.LimitAmount).HasPrecision(19, 4);

        // Jedan budžet po kategoriji i mjesecu; obrisan ne smeta novom
        builder.HasIndex(b => new { b.UserId, b.TransactionCategoryId, b.Year, b.Month })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(b => b.User).WithMany().HasForeignKey(b => b.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(b => b.TransactionCategory).WithMany().HasForeignKey(b => b.TransactionCategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(b => b.Currency).WithMany().HasForeignKey(b => b.CurrencyId).OnDelete(DeleteBehavior.Restrict);
    }
}
