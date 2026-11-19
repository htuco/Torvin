namespace Torvin.Model.Messages;

public static class QueueNames
{
    public const string Email = "email";

    // Poruke koje Worker nije uspio obraditi ni poslije svih pokušaja
    public const string EmailDeadLetter = "email.dlq";

    // Promjene statusa kartice. Red postoji od taska kartica, a poruke čekaju consumer u Workeru
    public const string CardStatus = "card-status";
    public const string CardStatusDeadLetter = "card-status.dlq";
}
