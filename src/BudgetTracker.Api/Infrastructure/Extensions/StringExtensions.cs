using System.Text.RegularExpressions;

namespace BudgetTracker.Api.Infrastructure.Extensions;

public static class StringExtensions
{
    public static string ExtractJsonFromCodeBlock(this string input)
    {
        if (!input.Contains("```json"))
            return input;

        var match = Regex.Match(input, @"```json\s*([\s\S]*?)\s*```");

        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        throw new FormatException("Could not extract JSON from the input string");
    }

    public static string ExtractJsonFromCodeBlock2(this string input)
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
