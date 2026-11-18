using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Torvin.Model.Enums;

namespace Torvin.Services.Database.Entities;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable(t => t.HasCheckConstraint("CK_Notifications_Type", EnumCheckConstraint.Sql<NotificationType>(nameof(Notification.Type))));

        builder.Property(n => n.Title).IsRequired().HasMaxLength(150);
        builder.Property(n => n.Message).IsRequired().HasMaxLength(1000);

        // Polling broja nepročitanih ide po ovom indeksu
        builder.HasIndex(n => new { n.UserId, n.IsRead });

        builder.HasOne(n => n.User).WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
