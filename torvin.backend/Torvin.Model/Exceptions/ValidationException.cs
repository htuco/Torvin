namespace Torvin.Model.Exceptions;

public sealed class ValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception(DefaultMessage)
{
    public const string DefaultMessage = "Podaci nisu ispravni.";

    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
