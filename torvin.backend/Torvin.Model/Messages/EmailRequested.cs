namespace Torvin.Model.Messages;

// Tekst maila sastavlja API, a Worker ga samo šalje. Kod za potvrdu je u tijelu, jer se iz hasha u bazi
// ne može vratiti. U bazi je i dalje samo hash, a kod istekne za najviše 30 min
public sealed record EmailRequested(Guid MessageId, string To, string Subject, string Body) : IMessage;
