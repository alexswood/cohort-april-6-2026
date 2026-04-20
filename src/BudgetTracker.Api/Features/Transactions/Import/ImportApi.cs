using System.Security.Claims;
using BudgetTracker.Api.AntiForgery;
using BudgetTracker.Api.Auth;
using BudgetTracker.Api.Infrastructure;
using BudgetTracker.Api.Features.Transactions.Import.Processing;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Features.Transactions.Import;

public static class ImportApi
{
    public static IEndpointRouteBuilder MapTransactionImportEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/import", ImportAsync)
            .DisableAntiforgery()
            .AddEndpointFilter<ConditionalAntiforgeryFilter>();

        routes.MapPost("/import/enhance", EnhanceAsync)
            .DisableAntiforgery()
            .AddEndpointFilter<ConditionalAntiforgeryFilter>();

        return routes;
    }

    private static async Task<Results<Ok<ImportResult>, BadRequest<string>>> ImportAsync(
        IFormFile file, [FromForm] string account,
        CsvImporter csvImporter, ITransactionEnhancer enhancer, BudgetTrackerContext context, ClaimsPrincipal claimsPrincipal)
    {
        var validationResult = ValidateFileInput(file, account);
        if (validationResult != null)
        {
            return validationResult;
        }

        try
        {
            var userId = claimsPrincipal.GetUserId();

            using var stream = file.OpenReadStream();
            var (result, transactions) = await csvImporter.ParseCsvAsync(stream, file.FileName, userId, account);

            if (!transactions.Any())
            {
                return TypedResults.Ok(result);
            }

            var sessionHash = Guid.NewGuid().ToString("N")[..16];

            foreach (var transaction in transactions)
            {
                transaction.ImportSessionHash = sessionHash;
            }

            await context.Transactions.AddRangeAsync(transactions);
            await context.SaveChangesAsync();

            var descriptions = transactions.Select(t => t.Description).ToList();
            var enhancements = await enhancer.EnhanceDescriptionsAsync(descriptions, account, userId, sessionHash);

            var enhancementResults = enhancements
                .Select((enhancement, index) => new TransactionEnhancementResult
                {
                    TransactionId = transactions[index].Id,
                    ImportSessionHash = sessionHash,
                    TransactionIndex = index,
                    OriginalDescription = enhancement.OriginalDescription,
                    EnhancedDescription = enhancement.EnhancedDescription,
                    SuggestedCategory = enhancement.SuggestedCategory,
                    ConfidenceScore = enhancement.ConfidenceScore
                })
                .ToList();

            result.ImportSessionHash = sessionHash;
            result.Enhancements = enhancementResults;

            return TypedResults.Ok(result);
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"Import failed: {ex.Message}");
        }
    }

    private static async Task<Results<Ok<EnhanceImportResult>, BadRequest<string>>> EnhanceAsync(
        [FromBody] EnhanceImportRequest request,
        BudgetTrackerContext context, ClaimsPrincipal claimsPrincipal)
    {
        if (string.IsNullOrWhiteSpace(request.ImportSessionHash))
        {
            return TypedResults.BadRequest("ImportSessionHash is required");
        }

        try
        {
            var userId = claimsPrincipal.GetUserId();

            var transactions = await context.Transactions
                .Where(t => t.ImportSessionHash == request.ImportSessionHash && t.UserId == userId)
                .ToListAsync();

            if (!transactions.Any())
            {
                return TypedResults.BadRequest($"No transactions found for session {request.ImportSessionHash}");
            }

            if (!request.ApplyEnhancements)
            {
                return TypedResults.Ok(new EnhanceImportResult
                {
                    ImportSessionHash = request.ImportSessionHash,
                    TotalTransactions = transactions.Count,
                    EnhancedCount = 0,
                    SkippedCount = transactions.Count
                });
            }

            var enhancedCount = 0;
            var skippedCount = 0;

            foreach (var enhancement in request.Enhancements)
            {
                if (enhancement.ConfidenceScore < request.MinConfidenceScore)
                {
                    skippedCount++;
                    continue;
                }

                var transaction = transactions.FirstOrDefault(t => t.Id == enhancement.TransactionId);
                if (transaction == null)
                {
                    skippedCount++;
                    continue;
                }

                transaction.Description = enhancement.EnhancedDescription;
                if (!string.IsNullOrWhiteSpace(enhancement.SuggestedCategory))
                {
                    transaction.Category = enhancement.SuggestedCategory;
                }

                enhancedCount++;
            }

            await context.SaveChangesAsync();

            return TypedResults.Ok(new EnhanceImportResult
            {
                ImportSessionHash = request.ImportSessionHash,
                TotalTransactions = transactions.Count,
                EnhancedCount = enhancedCount,
                SkippedCount = skippedCount
            });
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest($"Enhancement failed: {ex.Message}");
        }
    }

    private static BadRequest<string>? ValidateFileInput(IFormFile file, string account)
    {
        if (file == null || file.Length == 0)
        {
            return TypedResults.BadRequest("No file uploaded");
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return TypedResults.BadRequest("Only CSV files are supported");
        }

        if (file.Length > 10 * 1024 * 1024) // 10MB limit
        {
            return TypedResults.BadRequest("File size exceeds 10MB limit");
        }

        if (string.IsNullOrWhiteSpace(account))
        {
            return TypedResults.BadRequest("Account name is required");
        }

        return null;
    }
}
