using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Torvin.Model.Constants;

namespace Torvin.Services.Database.Entities;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.Property(r => r.Name).IsRequired().HasMaxLength(20);
        builder.HasIndex(r => r.Name).IsUnique();

        builder.HasData(
            new Role { Id = 1, Name = AppRoles.Admin, CreatedAt = ReferenceSeed.CreatedAt, ModifiedAt = ReferenceSeed.CreatedAt },
            new Role { Id = 2, Name = AppRoles.User, CreatedAt = ReferenceSeed.CreatedAt, ModifiedAt = ReferenceSeed.CreatedAt });
    }
}
