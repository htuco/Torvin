namespace Torvin.Model.Messages;

public static class QueueNames
{
    public const string Email = "email";

    // Poruke koje Worker nije uspio obraditi ni poslije svih pokušaja
    public const string EmailDeadLetter = "email.dlq";
}
