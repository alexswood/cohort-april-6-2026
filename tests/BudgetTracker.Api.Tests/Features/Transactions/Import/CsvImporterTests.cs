using System.Text;
using BudgetTracker.Api.Features.Transactions.Import.Processing;

namespace BudgetTracker.Api.Tests.Features.Transactions.Import;

public class CsvImporterTests
{
    private readonly CsvImporter _importer = new();

    private static Stream ToCsvStream(string csv) =>
        new MemoryStream(Encoding.UTF8.GetBytes(csv));

    [Fact]
    public async Task Should_parse_valid_csv_rows_into_transactions()
    {
        var csv = "Date,Description,Amount,Balance\n2024-01-15,Amazon Purchase,-45.67,1250.33\n2024-01-16,Coffee Shop,-5.89,1244.44";

        var (result, transactions) = await _importer.ParseCsvAsync(ToCsvStream(csv), "test.csv", "user1", "Checking");

        Assert.Equal(2, result.TotalRows);
        Assert.Equal(2, result.ImportedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Empty(result.Errors);
        Assert.Equal("test.csv", result.SourceFile);

        Assert.Equal(2, transactions.Count);
        Assert.All(transactions, t =>
        {
            Assert.Equal("user1", t.UserId);
            Assert.Equal("Checking", t.Account);
        });
        Assert.Equal("Amazon Purchase", transactions[0].Description);
        Assert.Equal(-45.67m, transactions[0].Amount);
        Assert.Equal(1250.33m, transactions[0].Balance);
    }

    [Fact]
    public async Task Should_handle_alternate_header_names()
    {
        var csv = "Transaction Date,Memo,Transaction Amount\n2024-01-15,Salary Deposit,2500.00";

        var (result, transactions) = await _importer.ParseCsvAsync(ToCsvStream(csv), "chase.csv", "user1", "Checking");

        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal("Salary Deposit", transactions[0].Description);
        Assert.Equal(2500.00m, transactions[0].Amount);
    }

    [Fact]
    public async Task Should_report_invalid_rows_and_continue()
    {
        // Second row has empty Description — should fail
        var csv = "Date,Description,Amount\n2024-01-15,Valid Transaction,-50.00\n2024-01-16,,-25.00";

        var (result, transactions) = await _importer.ParseCsvAsync(ToCsvStream(csv), "test.csv", "user1", "Checking");

        Assert.Equal(2, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(1, result.FailedCount);
        Assert.NotEmpty(result.Errors);
        Assert.Single(transactions);
        Assert.Equal("Valid Transaction", transactions[0].Description);
    }

    [Fact]
    public async Task Should_default_category_when_missing()
    {
        var csv = "Date,Description,Amount\n2024-01-15,Grocery Store,-89.45";

        var (_, transactions) = await _importer.ParseCsvAsync(ToCsvStream(csv), "test.csv", "user1", "Checking");

        Assert.Single(transactions);
        Assert.Equal("Uncategorized", transactions[0].Category);
    }

    [Fact]
    public async Task Should_parse_common_currency_formats()
    {
        var csv = "Date,Description,Amount\n2024-01-15,Big Purchase,-$1234.56";

        var (result, transactions) = await _importer.ParseCsvAsync(ToCsvStream(csv), "test.csv", "user1", "Checking");

        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(-1234.56m, transactions[0].Amount);
    }
}
