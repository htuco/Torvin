namespace Torvin.Model.Responses;

// Controller iz ID-a novog zapisa pravi Location header odgovora 201
public interface IHasId
{
    int Id { get; }
}
