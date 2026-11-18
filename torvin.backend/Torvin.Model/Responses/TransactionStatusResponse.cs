namespace Torvin.Model.Responses;

public sealed record TransactionStatusResponse(int Id, string Code, string Name) : IHasId;
