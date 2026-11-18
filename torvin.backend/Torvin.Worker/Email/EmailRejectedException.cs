namespace Torvin.Worker.Email;

// Trajna greška (neispravna adresa, SMTP server odbio primaoca): ponovni pokušaj ne može uspjeti
public sealed class EmailRejectedException(string reason, Exception inner) : Exception(reason, inner);
