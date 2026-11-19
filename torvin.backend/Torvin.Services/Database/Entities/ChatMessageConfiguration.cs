using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Torvin.Model.Enums;

namespace Torvin.Services.Database.Entities;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable(t => t.HasCheckConstraint("CK_ChatMessages_Role", EnumCheckConstraint.Sql<ChatMessageRole>(nameof(ChatMessage.Role))));

        builder.Property(m => m.Content).IsRequired().HasMaxLength(4000);

        builder.HasOne(m => m.User).WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
