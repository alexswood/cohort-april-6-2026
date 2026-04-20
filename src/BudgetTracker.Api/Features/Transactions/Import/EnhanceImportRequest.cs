namespace BudgetTracker.Api.Features.Transactions.Import;

public class EnhanceImportRequest
{
    public string ImportSessionHash { get; set; } = string.Empty;
    public List<TransactionEnhancementResult> Enhancements { get; set; } = new();
    public double MinConfidenceScore { get; set; } = 0.5;
    public bool ApplyEnhancements { get; set; } = true;
}
