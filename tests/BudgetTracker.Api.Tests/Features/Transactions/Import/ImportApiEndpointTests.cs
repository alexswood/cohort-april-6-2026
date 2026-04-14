using System.Text.Json;
using BudgetTracker.Api.Features.Transactions.Import;
using BudgetTracker.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Tests.Features.Transactions.Import;

[Collection("Database")]
public class ImportApiEndpointTests
{
    private readonly ApiFixture _fixture;

    public ImportApiEndpointTests(ApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Should_import_csv_via_endpoint_and_persist_transactions()
    {
        var client = _fixture.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var user = await _fixture.CreateTestUserAsync($"import_test_{Guid.NewGuid():N}@example.com");
        _fixture.AuthenticateClient(client, user.Id, user.Email!);

        var csvContent = "Date,Description,Amount,Balance\n2024-01-15,Test transaction,-50.00,1250.00";
        var fileBytes = System.Text.Encoding.UTF8.GetBytes(csvContent);

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("Checking"), "account");
        form.Add(new ByteArrayContent(fileBytes, 0, fileBytes.Length), "file", "test.csv");

        var antiforgeryResponse = await client.GetAsync("/api/antiforgery/token", TestContext.Current.CancellationToken);
        antiforgeryResponse.EnsureSuccessStatusCode();
        var tokenCookie = antiforgeryResponse.Headers.GetValues("Set-Cookie")
            .FirstOrDefault(c => c.StartsWith("XSRF-TOKEN="));
        Assert.NotNull(tokenCookie);
        var tokenValue = tokenCookie.Split(';').First().Split('=')[1];
        client.DefaultRequestHeaders.Add("RequestVerificationToken", tokenValue);

        var response = await client.PostAsync("/api/transactions/import", form, TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var result = JsonSerializer.Deserialize<ImportResult>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal(1, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Empty(result.Errors);
        Assert.Equal("test.csv", result.SourceFile);
        Assert.NotEqual(default(DateTime), result.ImportedAt);

        using var db = _fixture.CreateBudgetTrackerDbContext();
        var savedTransactions = await db.Transactions
            .Where(t => t.UserId == user.Id && t.Account == "Checking")
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Single(savedTransactions);
        Assert.Equal("Test transaction", savedTransactions[0].Description);
        Assert.Equal(-50.00m, savedTransactions[0].Amount);
    }

    [Fact]
    public async Task Should_return_import_result_with_errors_for_bad_rows()
    {
        var client = _fixture.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var user = await _fixture.CreateTestUserAsync($"import_bad_rows_{Guid.NewGuid():N}@example.com");
        _fixture.AuthenticateClient(client, user.Id, user.Email!);

        // Second row has an empty Description — should fail
        var csvContent = "Date,Description,Amount\n2024-01-15,Valid Transaction,-50.00\n2024-01-16,,-25.00";
        var fileBytes = System.Text.Encoding.UTF8.GetBytes(csvContent);

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("Savings"), "account");
        form.Add(new ByteArrayContent(fileBytes, 0, fileBytes.Length), "file", "partial.csv");

        var antiforgeryResponse = await client.GetAsync("/api/antiforgery/token", TestContext.Current.CancellationToken);
        antiforgeryResponse.EnsureSuccessStatusCode();
        var tokenCookie = antiforgeryResponse.Headers.GetValues("Set-Cookie")
            .FirstOrDefault(c => c.StartsWith("XSRF-TOKEN="));
        Assert.NotNull(tokenCookie);
        var tokenValue = tokenCookie.Split(';').First().Split('=')[1];
        client.DefaultRequestHeaders.Add("RequestVerificationToken", tokenValue);

        var response = await client.PostAsync("/api/transactions/import", form, TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var result = JsonSerializer.Deserialize<ImportResult>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(1, result.FailedCount);
        Assert.NotEmpty(result.Errors);

        using var db = _fixture.CreateBudgetTrackerDbContext();
        var savedTransactions = await db.Transactions
            .Where(t => t.UserId == user.Id && t.Account == "Savings")
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Single(savedTransactions);
        Assert.Equal("Valid Transaction", savedTransactions[0].Description);
    }
}
