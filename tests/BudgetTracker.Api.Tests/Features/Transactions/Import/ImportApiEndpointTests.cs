using System.Net;
using System.Text.Json;
using BudgetTracker.Api.Features.Transactions.Import;
using BudgetTracker.Api.Tests.Fixtures;

namespace BudgetTracker.Api.Tests.Features.Transactions.Import;

[Collection("Database")]
public class ImportApiEndpointTests
{
    private readonly ApiFixture _fixture;
    private readonly HttpClient _client;

    public ImportApiEndpointTests(ApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task Should_return_placeholder_import_result_for_valid_input()
    {
        var user = await _fixture.CreateTestUserAsync($"import_test_{Guid.NewGuid():N}@example.com");
        await _fixture.LoginAsync(_client, user.Email!, "Test123!");

        var csvContent = "Date,Description,Amount\n2024-01-15,Test transaction,-50.00";
        var fileBytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
        
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("Checking"), "account");
        form.Add(new ByteArrayContent(fileBytes, 0, fileBytes.Length), "file", "test.csv");

        var response = await _client.PostAsync("/api/transactions/import", form, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<ImportResult>(content, options);

        Assert.NotNull(result);
        Assert.Equal(0, result.TotalRows);
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Empty(result.Errors);
        Assert.Equal("test.csv", result.SourceFile);
        Assert.NotEqual(default(DateTime), result.ImportedAt);
    }
}
