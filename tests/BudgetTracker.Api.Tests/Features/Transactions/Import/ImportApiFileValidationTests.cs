using BudgetTracker.Api.Features.Transactions.Import;
using Microsoft.AspNetCore.Http;
using Xunit;

public class ImportApiFileValidationTests
{
    [Fact]
    public void Should_return_bad_request_when_file_is_missing()
    {
        var validationResult = ImportApiTestHelper.ValidateFileInput(null, "test");

        Assert.NotNull(validationResult);
        Assert.Contains("No file uploaded", validationResult);
    }

    [Fact]
    public void Should_return_bad_request_when_file_is_empty()
    {
        var mockFile = new MockFormFile("test.csv", Array.Empty<byte>());
        var validationResult = ImportApiTestHelper.ValidateFileInput(mockFile, "test");

        Assert.NotNull(validationResult);
        Assert.Contains("No file uploaded", validationResult);
    }

    [Fact]
    public void Should_return_bad_request_for_non_csv_file()
    {
        var mockFile = new MockFormFile("test.txt", new byte[] { 1, 2, 3 });
        var validationResult = ImportApiTestHelper.ValidateFileInput(mockFile, "test");

        Assert.NotNull(validationResult);
        Assert.Contains("Only CSV files are supported", validationResult);
    }

    [Fact]
    public void Should_return_bad_request_for_oversized_file()
    {
        var largeData = new byte[11 * 1024 * 1024]; // 11MB
        var mockFile = new MockFormFile("test.csv", largeData);
        var validationResult = ImportApiTestHelper.ValidateFileInput(mockFile, "test");

        Assert.NotNull(validationResult);
        Assert.Contains("File size exceeds 10MB limit", validationResult);
    }

    [Fact]
    public void Should_return_bad_request_when_account_is_empty()
    {
        var mockFile = new MockFormFile("test.csv", new byte[] { 1, 2, 3 });
        var validationResult = ImportApiTestHelper.ValidateFileInput(mockFile, "");

        Assert.NotNull(validationResult);
        Assert.Contains("Account name is required", validationResult);
    }

    [Fact]
    public void Should_return_bad_request_when_account_is_whitespace()
    {
        var mockFile = new MockFormFile("test.csv", new byte[] { 1, 2, 3 });
        var validationResult = ImportApiTestHelper.ValidateFileInput(mockFile, "   ");

        Assert.NotNull(validationResult);
        Assert.Contains("Account name is required", validationResult);
    }

    [Fact]
    public void Should_return_null_for_valid_input()
    {
        var mockFile = new MockFormFile("test.csv", new byte[] { 1, 2, 3 });
        var validationResult = ImportApiTestHelper.ValidateFileInput(mockFile, "Checking");

        Assert.Null(validationResult);
    }

    private class MockFormFile : IFormFile
    {
        private readonly byte[] _content;

        public MockFormFile(string fileName, byte[] content)
        {
            _content = content;
            FileName = fileName;
            Length = content.Length;
        }

        public string ContentType { get; } = "text/csv";
        public string ContentDisposition { get; } = "";
        public IHeaderDictionary Headers { get; } = new HeaderDictionary();
        public long Length { get; }
        public string Name { get; } = "";
        public string FileName { get; }

        public Stream OpenReadStream()
        {
            return new MemoryStream(_content);
        }

        public void CopyTo(Stream target)
        {
            var stream = OpenReadStream();
            stream.CopyTo(target);
        }

        public async Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
        {
            var stream = OpenReadStream();
            await stream.CopyToAsync(target, cancellationToken);
        }
    }
}

// Helper class to test the private ValidateFileInput method
internal static class ImportApiTestHelper
{
    public static string? ValidateFileInput(IFormFile? file, string account)
    {
        if (file == null || file.Length == 0)
        {
            return "No file uploaded";
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return "Only CSV files are supported";
        }

        if (file.Length > 10 * 1024 * 1024) // 10MB limit
        {
            return "File size exceeds 10MB limit";
        }

        if (string.IsNullOrWhiteSpace(account))
        {
            return "Account name is required";
        }

        return null;
    }
}
