namespace Torvin.Model.Responses;

public sealed record CardTypeResponse(int Id, string Name, string? Description) : IHasId;
