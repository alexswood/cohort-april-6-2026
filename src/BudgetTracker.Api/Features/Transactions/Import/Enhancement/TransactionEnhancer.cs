using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace BudgetTracker.Api.Features.Transactions.Import.Processing;

public class TransactionEnhancer : ITransactionEnhancer
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<TransactionEnhancer> _logger;

    public TransactionEnhancer(IChatClient chatClient, ILogger<TransactionEnhancer> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task<List<EnhancedTransactionDescription>> EnhanceDescriptionsAsync(
        List<string> descriptions,
        string account,
        string userId,
        string? currentImportSessionHash = null)
    {
        if (!descriptions.Any())
        {
            return new List<EnhancedTransactionDescription>();
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var systemPrompt = BuildSystemPrompt();
            var userMessage = BuildUserMessage(descriptions, account);

            var response = await _chatClient.GetResponseAsync(
                messages: [
                    new ChatMessage(ChatRole.System, systemPrompt),
                    new ChatMessage(ChatRole.User, userMessage)
                ]);

            var content = response.Text ?? string.Empty;
            var result = ParseResponse(content, descriptions);

            _logger.LogInformation("AI processing completed in {ProcessingTime}ms", stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enhance transaction descriptions");
            return descriptions.Select(d => new EnhancedTransactionDescription
            {
                OriginalDescription = d,
                EnhancedDescription = d,
                SuggestedCategory = null,
                ConfidenceScore = 0.0
            }).ToList();
        }
    }

    private string BuildSystemPrompt()
    {
        return """
Enhance transaction descriptions and suggest categories.

Your task:
1. Clarify and expand the transaction description to be more informative
2. Suggest a category from: "Groceries", "Dining", "Transport", "Utilities", "Entertainment",
        "Healthcare", "Shopping", "Income", "Transfer", "Other"
3. Provide a confidence score (0.0-1.0) for your suggestion

Return a JSON array with this structure:
[
  {{
    "originalDescription": "original text",
    "enhancedDescription": "clarified and expanded text",
    "suggestedCategory": "Category or null",
    "confidenceScore": 0.85
  }}
]

Example input:
["AMAZON PURCHASE", "COFFEE SHOP", "SALARY DEPOSIT"]

Example output:
[
  {{
    "originalDescription": "AMAZON PURCHASE",
    "enhancedDescription": "Amazon marketplace purchase",
    "suggestedCategory": "Shopping",
    "confidenceScore": 0.8
  }},
  {{
    "originalDescription": "COFFEE SHOP",
    "enhancedDescription": "Coffee shop - Dining",
    "suggestedCategory": "Dining",
    "confidenceScore": 0.9
  }},
  {{
    "originalDescription": "SALARY DEPOSIT",
    "enhancedDescription": "Monthly salary deposit",
    "suggestedCategory": "Income",
    "confidenceScore": 0.95
  }}
]

Respond ONLY with the JSON array, no additional text.
""";
    }

    private string BuildUserMessage(List<string> descriptions, string account)
    {
        var jsonArray = JsonSerializer.Serialize(descriptions);
        return $"Account: {account}\n\nTransactions:\n{jsonArray}";
    }

    private List<EnhancedTransactionDescription> ParseResponse(
        string content,
        List<string> originalDescriptions)
    {
        try
        {
            var jsonContent = ExtractJsonFromCodeBlock(content);
            var enhancedDescriptions = JsonSerializer.Deserialize<List<EnhancedTransactionDescription>>(
                jsonContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (enhancedDescriptions?.Count == originalDescriptions.Count)
            {
                return enhancedDescriptions;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI response as JSON: {Content}", content);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Failed to extract JSON from AI response");
        }

        _logger.LogWarning("AI response format was invalid, returning original descriptions");

        return originalDescriptions.Select(d => new EnhancedTransactionDescription
        {
            OriginalDescription = d,
            EnhancedDescription = d,
            SuggestedCategory = null,
            ConfidenceScore = 0.0
        }).ToList();
    }

    private static string ExtractJsonFromCodeBlock(string input)
    {
        // Look for content between ```json and ``` markers
        var match = Regex.Match(input, @"```json\s*([\s\S]*?)\s*```");

        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        // Try to find a JSON array directly
        var arrayMatch = Regex.Match(input, @"\[[\s\S]*\]");
        if (arrayMatch.Success)
        {
            return arrayMatch.Value;
        }

        throw new FormatException("Could not extract JSON from the input string");
    }
}
