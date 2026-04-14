using BudgetTracker.Api.Features.Transactions.List;
using Xunit;

public class PagedResultTests
{
    [Fact]
    public void Should_calculate_total_pages_and_navigation_flags()
    {
        var result = new PagedResult<string>
        {
            TotalCount = 45,
            Page = 2,
            PageSize = 20
        };

        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
    }

    [Fact]
    public void Should_return_zero_total_pages_when_total_count_is_zero()
    {
        var result = new PagedResult<string>
        {
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };

        Assert.Equal(0, result.TotalPages);
        Assert.False(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
    }
}
