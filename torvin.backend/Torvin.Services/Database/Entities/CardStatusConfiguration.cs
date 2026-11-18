using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Torvin.Model.Constants;

namespace Torvin.Services.Database.Entities;

public class CardStatusConfiguration : IEntityTypeConfiguration<CardStatus>
{
    public void Configure(EntityTypeBuilder<CardStatus> builder)
    {
        builder.Property(s => s.Code).IsRequired().HasMaxLength(30);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Description).HasMaxLength(200);
        builder.HasIndex(s => s.Code).IsUnique();

        builder.HasData(
            Create(1, CardStatusCodes.Pending, "Na čekanju", "Kartica čeka potvrdu kodom iz emaila."),
            Create(2, CardStatusCodes.Active, "Aktivna", "Kartica se može koristiti."),
            Create(3, CardStatusCodes.Blocked, "Blokirana", "Kartica je privremeno blokirana."),
            Create(4, CardStatusCodes.Lost, "Izgubljena", "Kartica je prijavljena kao izgubljena."),
            Create(5, CardStatusCodes.Stolen, "Ukradena", "Kartica je prijavljena kao ukradena."),
            Create(6, CardStatusCodes.Closed, "Zatvorena", "Kartica je trajno zatvorena."));
    }

    private static CardStatus Create(int id, string code, string name, string description) => new()
    {
        Id = id, Code = code, Name = name, Description = description,
        CreatedAt = ReferenceSeed.CreatedAt, ModifiedAt = ReferenceSeed.CreatedAt,
    };
}
