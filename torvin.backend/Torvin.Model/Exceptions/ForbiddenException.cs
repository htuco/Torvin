namespace Torvin.Model.Exceptions;

public sealed class ForbiddenException(string message) : Exception(message);
