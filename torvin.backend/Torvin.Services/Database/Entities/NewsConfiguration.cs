using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Torvin.Services.Database.Entities;

public class NewsConfiguration : IEntityTypeConfiguration<News>
{
    public void Configure(EntityTypeBuilder<News> builder)
    {
        builder.HasQueryFilter(n => !n.IsDeleted);

        builder.Property(n => n.Title).IsRequired().HasMaxLength(150);
        builder.Property(n => n.Content).IsRequired().HasMaxLength(4000);
        builder.Property(n => n.Image).IsRequired();
        builder.Property(n => n.ImageContentType).IsRequired().HasMaxLength(50);

        builder.HasOne(n => n.AuthorUser).WithMany().HasForeignKey(n => n.AuthorUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
