namespace Torvin.Model.Responses;

public sealed record TransactionTypeResponse(int Id, string Code, string Name) : IHasId;
