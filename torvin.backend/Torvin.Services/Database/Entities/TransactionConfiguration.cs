using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Torvin.Services.Database.Entities;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable(t => t.HasCheckConstraint("CK_Transactions_Amount", "[Amount] > 0"));

        builder.Property(t => t.ReferenceNumber).IsRequired().HasMaxLength(20).IsUnicode(false);
        builder.Property(t => t.Amount).HasPrecision(19, 4);
        builder.Property(t => t.Note).HasMaxLength(200);

        builder.HasIndex(t => t.ReferenceNumber).IsUnique();

        builder.HasOne(t => t.SenderUser).WithMany().HasForeignKey(t => t.SenderUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.ReceiverUser).WithMany().HasForeignKey(t => t.ReceiverUserId).OnDelete(DeleteBehavior.Restrict);
        // Kartica se ne briše nego ide u status Closed, pa transakcija uvijek zadrži svoju karticu.
        // SET NULL nije moguć jer SQL Server odbija dva takva FK-a prema Cards (multiple cascade paths)
        builder.HasOne(t => t.SenderCard).WithMany().HasForeignKey(t => t.SenderCardId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.ReceiverCard).WithMany().HasForeignKey(t => t.ReceiverCardId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.Currency).WithMany().HasForeignKey(t => t.CurrencyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.TransactionType).WithMany().HasForeignKey(t => t.TransactionTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.TransactionStatus).WithMany().HasForeignKey(t => t.TransactionStatusId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.TransactionCategory).WithMany().HasForeignKey(t => t.TransactionCategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.OriginalTransaction).WithMany().HasForeignKey(t => t.OriginalTransactionId).OnDelete(DeleteBehavior.Restrict);
    }
}
