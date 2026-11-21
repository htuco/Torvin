using Torvin.Model.Enums;

namespace Torvin.Model.SearchObjects;

// Fts traži po napomeni, broju reference i imenu druge strane. Datumi su uključivi, a CardId je moja kartica
public class TransactionSearchObject : BaseSearchObject
{
    public DateOnly? DateFrom { get; set; }

    public DateOnly? DateTo { get; set; }

    public int? TransactionTypeId { get; set; }

    public int? TransactionStatusId { get; set; }

    public int? CardId { get; set; }

    public decimal? AmountFrom { get; set; }

    public decimal? AmountTo { get; set; }

    public TransactionDirection? Direction { get; set; }
}
