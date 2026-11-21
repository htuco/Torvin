namespace Torvin.Model.Responses;

// Korisnik kojem se može poslati novac. Email se ne vraća, da pretraga ne otkriva adrese
public sealed record RecipientResponse(int UserId, string FirstName, string LastName, string Username);
