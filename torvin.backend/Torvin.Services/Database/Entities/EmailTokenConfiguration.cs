using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Torvin.Model.Enums;

namespace Torvin.Services.Database.Entities;

public class EmailTokenConfiguration : IEntityTypeConfiguration<EmailToken>
{
    public void Configure(EntityTypeBuilder<EmailToken> builder)
    {
        builder.ToTable(t => t.HasCheckConstraint("CK_EmailTokens_Purpose", EnumCheckConstraint.Sql<EmailTokenPurpose>(nameof(EmailToken.Purpose))));

        builder.Property(t => t.CodeHash).IsRequired().HasMaxLength(256);

        builder.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.Card).WithMany().HasForeignKey(t => t.CardId).OnDelete(DeleteBehavior.Restrict);
    }
}
