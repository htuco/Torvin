namespace Torvin.Model.Messages;

// Događaji koje čita više consumera. Svaki consumer ima svoj red vezan za exchange, pa svaki dobije svaku poruku
public static class ExchangeNames
{
    public const string TransactionCompleted = "transaction-completed";
}
