namespace Torvin.Model.Responses;

public sealed record CurrencyResponse(int Id, string Code, string Name, string Symbol) : IHasId;
