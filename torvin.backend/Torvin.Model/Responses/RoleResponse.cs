namespace Torvin.Model.Responses;

public sealed record RoleResponse(int Id, string Name) : IHasId;
