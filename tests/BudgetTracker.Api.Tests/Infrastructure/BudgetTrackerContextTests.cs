using BudgetTracker.Api.Auth;
using BudgetTracker.Api.Features.Transactions;
using BudgetTracker.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class BudgetTrackerContextTests
{
    [Fact]
    public void Should_have_transaction_dbset()
    {
        var options = new DbContextOptionsBuilder<BudgetTrackerContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;

        using var context = new BudgetTrackerContext(options);
        
        // Check that DbSet exists
        Assert.NotNull(context.Transactions);
    }

    [Fact]
    public void Should_configure_transaction_entity()
    {
        var options = new DbContextOptionsBuilder<BudgetTrackerContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;

        using var context = new BudgetTrackerContext(options);
        var model = context.Model;
        var transactionEntity = model.FindEntityType(typeof(Transaction));

        Assert.NotNull(transactionEntity);
        
        // Check that the entity has the expected properties
        var properties = transactionEntity.GetProperties().Select(p => p.Name).ToList();
        Assert.Contains("Id", properties);
        Assert.Contains("Date", properties);
        Assert.Contains("Description", properties);
        Assert.Contains("Amount", properties);
        Assert.Contains("UserId", properties);
    }
}