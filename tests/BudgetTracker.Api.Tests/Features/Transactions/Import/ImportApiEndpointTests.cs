using System.Net;
using System.Text.Json;
using BudgetTracker.Api.Features.Transactions.Import;
using BudgetTracker.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;

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
    public async Task Should_return_placeholder_import_result_for_valid_input()
    {
        var client = _fixture.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var user = await _fixture.CreateTestUserAsync($"import_test_{Guid.NewGuid():N}@example.com");
        _fixture.AuthenticateClient(client, user.Id, user.Email!);

        var csvContent = "Date,Description,Amount\n2024-01-15,Test transaction,-50.00";
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
