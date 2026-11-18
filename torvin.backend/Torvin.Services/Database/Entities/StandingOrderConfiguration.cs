using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Torvin.Model.Enums;

namespace Torvin.Services.Database.Entities;

public class StandingOrderConfiguration : IEntityTypeConfiguration<StandingOrder>
{
    public void Configure(EntityTypeBuilder<StandingOrder> builder)
    {
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_StandingOrders_Amount", "[Amount] > 0");
            t.HasCheckConstraint("CK_StandingOrders_Frequency", EnumCheckConstraint.Sql<StandingOrderFrequency>(nameof(StandingOrder.Frequency)));
            t.HasCheckConstraint("CK_StandingOrders_Status", EnumCheckConstraint.Sql<StandingOrderStatus>(nameof(StandingOrder.Status)));
        });

        builder.Property(o => o.Amount).HasPrecision(19, 4);
        builder.Property(o => o.Note).HasMaxLength(200);

        builder.HasOne(o => o.User).WithMany().HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.SourceCard).WithMany().HasForeignKey(o => o.SourceCardId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.RecipientUser).WithMany().HasForeignKey(o => o.RecipientUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
