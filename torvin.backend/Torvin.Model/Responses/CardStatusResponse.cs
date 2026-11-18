namespace Torvin.Model.Responses;

public sealed record CardStatusResponse(int Id, string Code, string Name, string? Description) : IHasId;
