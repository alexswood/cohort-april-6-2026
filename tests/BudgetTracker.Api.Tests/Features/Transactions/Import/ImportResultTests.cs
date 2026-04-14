using BudgetTracker.Api.Features.Transactions.Import;
using Xunit;

public class ImportResultTests
{
    [Fact]
    public void Should_initialize_errors_list()
    {
        var result = new ImportResult();

        Assert.NotNull(result.Errors);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Should_store_source_file_and_imported_at()
    {
        var fileName = "test-transactions.csv";
        var importedAt = DateTime.UtcNow;

        var result = new ImportResult
        {
            SourceFile = fileName,
            ImportedAt = importedAt,
            TotalRows = 10,
            ImportedCount = 8,
            FailedCount = 2
        };

        Assert.Equal(fileName, result.SourceFile);
        Assert.Equal(importedAt, result.ImportedAt);
        Assert.Equal(10, result.TotalRows);
        Assert.Equal(8, result.ImportedCount);
        Assert.Equal(2, result.FailedCount);
    }
}
