namespace BudgetTracker.Api.Features.Transactions.Import;

public class ImportResult
{
    public int TotalRows { get; set; }
    public int ImportedCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public string? SourceFile { get; set; }
    public string ImportSessionHash { get; set; } = string.Empty;
    public DateTime ImportedAt { get; set; }
    public List<TransactionEnhancementResult> Enhancements { get; set; } = new();
}
