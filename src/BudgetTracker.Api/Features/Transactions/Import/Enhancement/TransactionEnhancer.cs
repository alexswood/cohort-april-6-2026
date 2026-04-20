using System.Text.Json;
using Microsoft.Extensions.AI;

namespace BudgetTracker.Api.Features.Transactions.Import.Processing;

public class TransactionEnhancer : ITransactionEnhancer
{
    private readonly IChatClient _chatClient;

    private static readonly List<string> PredefinedCategories = new()
    {
        "Groceries", "Dining", "Transport", "Utilities", "Entertainment",
        "Healthcare", "Shopping", "Income", "Transfer", "Other"
    };

    public TransactionEnhancer(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public async Task<List<EnhancedTransactionDescription>> EnhanceDescriptionsAsync(
        List<string> descriptions,
        string account,
        string userId,
        string? currentImportSessionHash = null)
    {
        try
        {
            var systemPrompt = BuildSystemPrompt();
            var userMessage = BuildUserMessage(descriptions, account);

            var response = await _chatClient.GetResponseAsync(
                messages: [
                    new ChatMessage(ChatRole.System, systemPrompt),
                    new ChatMessage(ChatRole.User, userMessage)
                ]);

            var result = ParseResponse(response.ToString() ?? string.Empty);
            return result;
        }
        catch
        {
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
        var categoriesList = string.Join(", ", PredefinedCategories);

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

    private List<EnhancedTransactionDescription> ParseResponse(string response)
    {
        var result = new List<EnhancedTransactionDescription>();

        try
        {
            var trimmed = response.Trim();
            if (trimmed.StartsWith("```json"))
            {
                trimmed = trimmed["```json".Length..];
            }
            if (trimmed.StartsWith("```"))
            {
                trimmed = trimmed[3..];
            }
            if (trimmed.EndsWith("```"))
            {
                trimmed = trimmed[..^3];
            }

            var items = JsonSerializer.Deserialize<List<JsonElement>>(trimmed.Trim());

            if (items == null) return result;

            foreach (var item in items)
            {
                var enhanced = new EnhancedTransactionDescription
                {
                    OriginalDescription = item.TryGetProperty("originalDescription", out var orig)
                        ? orig.GetString() ?? string.Empty
                        : string.Empty,
                    EnhancedDescription = item.TryGetProperty("enhancedDescription", out var enh)
                        ? enh.GetString() ?? string.Empty
                        : string.Empty,
                    SuggestedCategory = item.TryGetProperty("suggestedCategory", out var cat)
                        ? cat.GetString()
                        : null,
                    ConfidenceScore = item.TryGetProperty("confidenceScore", out var conf)
                        ? conf.GetDouble()
                        : 0.0
                };

                result.Add(enhanced);
            }
        }
        catch
        {
            return result;
        }

        return result;
    }
}
