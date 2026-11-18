using System.Text.Json.Serialization;

namespace Torvin.Model.Responses;

public sealed record ErrorResponse(
    string Message,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyDictionary<string, string[]>? Errors = null);
