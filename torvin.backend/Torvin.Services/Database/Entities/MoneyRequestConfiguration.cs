using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Torvin.Model.Enums;

namespace Torvin.Services.Database.Entities;

public class MoneyRequestConfiguration : IEntityTypeConfiguration<MoneyRequest>
{
    public void Configure(EntityTypeBuilder<MoneyRequest> builder)
    {
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_MoneyRequests_Amount", "[Amount] > 0");
            t.HasCheckConstraint("CK_MoneyRequests_Status", EnumCheckConstraint.Sql<MoneyRequestStatus>(nameof(MoneyRequest.Status)));
        });

        builder.Property(r => r.Amount).HasPrecision(19, 4);
        builder.Property(r => r.Note).HasMaxLength(200);

        // Jedna transakcija plaća najviše jedan zahtjev
        builder.HasIndex(r => r.TransactionId).IsUnique().HasFilter("[TransactionId] IS NOT NULL");

        builder.HasOne(r => r.RequesterUser).WithMany().HasForeignKey(r => r.RequesterUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.PayerUser).WithMany().HasForeignKey(r => r.PayerUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Currency).WithMany().HasForeignKey(r => r.CurrencyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Transaction).WithMany().HasForeignKey(r => r.TransactionId).OnDelete(DeleteBehavior.Restrict);
    }
}
