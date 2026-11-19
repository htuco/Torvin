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

        // Istovremeni pokušaji s istim kodom: upis uspijeva samo ako niko nije promijenio brojač ili iskoristio
        // kod u međuvremenu, pa paralelni zahtjevi ne dobijaju više od 5 pokušaja niti dvaput troše isti kod
        builder.Property(t => t.AttemptCount).IsConcurrencyToken();
        builder.Property(t => t.UsedAt).IsConcurrencyToken();

        builder.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.Card).WithMany().HasForeignKey(t => t.CardId).OnDelete(DeleteBehavior.Restrict);
    }
}
