using BudgetTracker.Api.Features.Transactions;
using Xunit;

public class TransactionTypesTests
{
    [Fact]
    public void Should_map_transaction_to_dto_correctly()
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Date = new DateTime(2024, 1, 15),
            Description = "Coffee purchase",
            Amount = -5.50m,
            Balance = 1000.00m,
            Category = "Food",
            Labels = "morning,starbucks",
            ImportedAt = DateTime.UtcNow,
            Account = "Checking",
            UserId = "user123"
        };

        var dto = transaction.MapToDto();

        Assert.Equal(transaction.Id, dto.Id);
        Assert.Equal(transaction.Date, dto.Date);
        Assert.Equal(transaction.Description, dto.Description);
        Assert.Equal(transaction.Amount, dto.Amount);
        Assert.Equal(transaction.Balance, dto.Balance);
        Assert.Equal(transaction.Category, dto.Category);
        Assert.Equal(transaction.Labels, dto.Labels);
        Assert.Equal(transaction.ImportedAt, dto.ImportedAt);
        Assert.Equal(transaction.Account, dto.Account);
    }

    [Fact]
    public void Should_handle_null_values_in_mapping()
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Date = DateTime.UtcNow,
            Description = "Test",
            Amount = 10.00m,
            Balance = null,
            Category = null,
            Labels = null,
            ImportedAt = DateTime.UtcNow,
            Account = "Test Account",
            UserId = "user123"
        };

        var dto = transaction.MapToDto();

        Assert.Null(dto.Balance);
        Assert.Null(dto.Category);
        Assert.Null(dto.Labels);
    }
}