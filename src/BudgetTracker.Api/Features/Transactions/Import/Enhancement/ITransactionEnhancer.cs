namespace BudgetTracker.Api.Features.Transactions.Import.Processing;

public interface ITransactionEnhancer
{
    Task<List<EnhancedTransactionDescription>> EnhanceDescriptionsAsync(
        List<string> descriptions,
        string account,
        string userId);
}
