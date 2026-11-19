namespace Torvin.Services.Database;

// Samo za zapise koje korisnik ili admin briše. Korisnici i kartice mijenjaju status, a historija se ne briše
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}
