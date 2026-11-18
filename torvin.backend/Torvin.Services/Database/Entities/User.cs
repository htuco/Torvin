namespace Torvin.Services.Database.Entities;

public class User : BaseEntity
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string? PhoneNumber { get; set; }

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    // Admin blokira račun promjenom ovog polja, korisnik se nikad ne briše
    public bool IsActive { get; set; } = true;
    public bool EmailConfirmed { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockoutEndAt { get; set; }

    public byte[]? ProfileImage { get; set; }
    public string? ProfileImageContentType { get; set; }

    public ICollection<Card> Cards { get; set; } = [];
}
