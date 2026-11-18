namespace Torvin.Services.Database;

// Audit polja postavlja AuditFieldsInterceptor, ne servisi
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}
