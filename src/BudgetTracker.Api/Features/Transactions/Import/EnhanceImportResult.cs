namespace BudgetTracker.Api.Features.Transactions.Import;

public class EnhanceImportResult
{
    public string ImportSessionHash { get; set; } = string.Empty;
    public int TotalTransactions { get; set; }
    public int EnhancedCount { get; set; }
    public int SkippedCount { get; set; }
}
