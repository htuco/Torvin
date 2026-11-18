namespace Torvin.Model.Responses;

public sealed record TransactionCategoryResponse(int Id, string Name, string? Description) : IHasId;
