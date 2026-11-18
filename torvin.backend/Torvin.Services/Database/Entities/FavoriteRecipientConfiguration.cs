using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Torvin.Services.Database.Entities;

public class FavoriteRecipientConfiguration : IEntityTypeConfiguration<FavoriteRecipient>
{
    public void Configure(EntityTypeBuilder<FavoriteRecipient> builder)
    {
        builder.HasQueryFilter(f => !f.IsDeleted);

        builder.Property(f => f.Nickname).HasMaxLength(50);

        builder.HasIndex(f => new { f.UserId, f.RecipientUserId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(f => f.User).WithMany().HasForeignKey(f => f.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(f => f.RecipientUser).WithMany().HasForeignKey(f => f.RecipientUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
