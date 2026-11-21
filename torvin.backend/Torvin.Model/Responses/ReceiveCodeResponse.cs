namespace Torvin.Model.Responses;

// Code je tekst koji mobilna aplikacija prikaže kao QR. Poslije ExpiresAt treba tražiti novi
public sealed record ReceiveCodeResponse(string Code, decimal? Amount, DateTime ExpiresAt);
