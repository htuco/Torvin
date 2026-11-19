namespace Torvin.Model.Messages;

// Svaka promjena statusa kartice. Worker iz baze čita vlasnika i karticu, a poruka nosi samo ono što se iz baze
// poslije ne vidi: stari status, ko je promijenio i razlog
public sealed record CardStatusChanged(
    Guid MessageId,
    int CardId,
    string OldStatusCode,
    string NewStatusCode,
    int ChangedByUserId,
    string? Reason) : IMessage;
