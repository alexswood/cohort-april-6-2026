namespace BudgetTracker.Api.Features.Transactions.Import;

public class TransactionEnhancementResult
{
    public Guid TransactionId { get; set; }
    public string ImportSessionHash { get; set; } = string.Empty;
    public int TransactionIndex { get; set; }
    public string OriginalDescription { get; set; } = string.Empty;
    public string EnhancedDescription { get; set; } = string.Empty;
    public string? SuggestedCategory { get; set; }
    public double ConfidenceScore { get; set; }
}
