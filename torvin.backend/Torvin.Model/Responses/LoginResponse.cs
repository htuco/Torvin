namespace Torvin.Model.Responses;

public sealed record LoginResponse(string Token, DateTime ExpiresAt, string Role, string FirstName, string LastName);
