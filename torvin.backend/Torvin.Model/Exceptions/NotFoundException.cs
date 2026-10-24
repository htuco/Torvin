namespace Torvin.Model.Exceptions;

public sealed class NotFoundException(string message) : Exception(message);
