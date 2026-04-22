namespace BudgetTracker.Api.Features.Transactions.Import.Processing;

public class EnhancedTransactionDescription
{
    public string OriginalDescription { get; set; } = string.Empty;
    public string EnhancedDescription { get; set; } = string.Empty;
    public string? SuggestedCategory { get; set; }
    public double ConfidenceScore { get; set; }
}
