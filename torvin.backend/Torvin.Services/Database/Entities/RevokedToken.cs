namespace Torvin.Services.Database.Entities;

// Opozvan JWT (odjava) važi dok mu ne istekne rok, poslije toga se zapis može očistiti
public class RevokedToken : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Jti { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
}
