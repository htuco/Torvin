namespace Torvin.Services.Database.Entities;

// Trenutna lozinka je najnoviji zapis, a stariji služe za zabranu ponavljanja
public class UserPassword : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string PasswordHash { get; set; } = "";
}
