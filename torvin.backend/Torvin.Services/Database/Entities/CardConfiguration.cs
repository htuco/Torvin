using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Torvin.Model.Enums;

namespace Torvin.Services.Database.Entities;

public class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Cards_Balance", "[Balance] >= 0");
            t.HasCheckConstraint("CK_Cards_Network", EnumCheckConstraint.Sql<CardNetwork>(nameof(Card.Network)));
        });

        builder.Property(c => c.Name).HasMaxLength(50);
        builder.Property(c => c.Last4Digits).IsRequired().HasMaxLength(4).IsFixedLength();
        builder.Property(c => c.Balance).HasPrecision(19, 4);
        builder.Property(c => c.RowVersion).IsRowVersion();

        // Najviše jedna podrazumijevana kartica po korisniku
        builder.HasIndex(c => c.UserId).IsUnique().HasFilter("[IsDefault] = 1");

        builder.HasOne(c => c.User).WithMany(u => u.Cards).HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.CardType).WithMany().HasForeignKey(c => c.CardTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.CardStatus).WithMany().HasForeignKey(c => c.CardStatusId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.Currency).WithMany().HasForeignKey(c => c.CurrencyId).OnDelete(DeleteBehavior.Restrict);
    }
}
