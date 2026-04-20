# Week 3 Part 1 — Intelligent Import

## Overview

The import flow becomes two-step:
1. **`POST /api/transactions/import`** — Parse CSV, run AI enhancements, save transactions with original descriptions. Returns suggestions for review.
2. **`POST /api/transactions/import/enhance`** — User approves suggestions. Backend applies them and saves to DB.

---

## Data Model Changes

Add to `Transaction` entity in `TransactionTypes.cs`:
```csharp
[MaxLength(50)]
public string? ImportSessionHash { get; set; }
```

Create EF migration: `AddImportSessionHashToTransaction`

---

## New Types

### `Import/Processing/EnhancedTransactionDescription.cs`
Returned by `ITransactionEnhancer`. Internal to the processing pipeline.
```csharp
public class EnhancedTransactionDescription
{
    public string OriginalDescription { get; set; }
    public string EnhancedDescription { get; set; }
    public string? SuggestedCategory { get; set; }
    public double ConfidenceScore { get; set; }
}
```

### `Import/TransactionEnhancementResult.cs`
API-level result. Adds transaction identity fields to the enhancer output.
```csharp
public class TransactionEnhancementResult
{
    public Guid TransactionId { get; set; }
    public string ImportSessionHash { get; set; } = string.Empty;
    public int TransactionIndex { get; set; }
    public string OriginalDescription { get; set; } = string.Empty;
    public string EnhancedDescription { get; set; } = string.Empty;
    public string? SuggestedCategory { get; set; }
    public double ConfidenceScore { get; set; }
}
```

### `Import/EnhanceImportRequest.cs`
Request body for `POST /api/transactions/import/enhance`.
```csharp
public class EnhanceImportRequest
{
    public string ImportSessionHash { get; set; } = string.Empty;
    public List<TransactionEnhancementResult> Enhancements { get; set; } = new();
    public double MinConfidenceScore { get; set; } = 0.5;
    public bool ApplyEnhancements { get; set; } = true;
}
```

### `Import/EnhanceImportResult.cs`
Response from `POST /api/transactions/import/enhance`.
```csharp
public class EnhanceImportResult
{
    public string ImportSessionHash { get; set; } = string.Empty;
    public int TotalTransactions { get; set; }
    public int EnhancedCount { get; set; }
    public int SkippedCount { get; set; }
}
```

### Updated `Import/ImportResult.cs`
Add two properties:
```csharp
public string ImportSessionHash { get; set; } = string.Empty;
public List<TransactionEnhancementResult> Enhancements { get; set; } = new();
```

---

## `ITransactionEnhancer` + `TransactionEnhancer`

### `Import/Processing/ITransactionEnhancer.cs`
```csharp
public interface ITransactionEnhancer
{
    Task<List<EnhancedTransactionDescription>> EnhanceDescriptionsAsync(
        List<string> descriptions,
        string account,
        string userId,
        string? currentImportSessionHash = null);
}
```

### `Import/Processing/TransactionEnhancer.cs`
- Depends on `IChatClient` (already registered in DI)
- System prompt instructs the model to: enhance descriptions, suggest a category from the predefined list, return a JSON array
- Predefined categories: `Groceries`, `Dining`, `Transport`, `Utilities`, `Entertainment`, `Healthcare`, `Shopping`, `Income`, `Transfer`, `Other`
- System prompt includes: an input/output example, the category list, and the exact JSON schema to return
- On any AI failure: returns original descriptions with empty `EnhancedDescription`, `null` category, `0.0` confidence (graceful fallback, never throws)

---

## Updated `POST /api/transactions/import`

1. Validate file input (existing logic)
2. Parse CSV → `List<Transaction>` with GUIDs assigned
3. Generate `ImportSessionHash = Guid.NewGuid().ToString("N")[..16]`; set on every transaction
4. Save transactions to DB with **original descriptions** and session hash
5. Call `EnhanceDescriptionsAsync(descriptions, account, userId, sessionHash)`
6. Map `List<EnhancedTransactionDescription>` → `List<TransactionEnhancementResult>` (add `TransactionId` by index, `TransactionIndex`, `ImportSessionHash`)
7. Return `ImportResult { ImportSessionHash, TotalRows, ImportedCount, FailedCount, Enhancements }`

---

## New `POST /api/transactions/import/enhance`

1. Receive `EnhanceImportRequest { ImportSessionHash, Enhancements, MinConfidenceScore, ApplyEnhancements }`
2. Load transactions from DB: `WHERE ImportSessionHash = ? AND UserId = ?` — validates ownership
3. Return `BadRequest` if no transactions found for that hash + user
4. If `ApplyEnhancements == true`: for each enhancement where `ConfidenceScore >= MinConfidenceScore`, find matching transaction by `TransactionId` and update `Description` + `Category`
5. `SaveChangesAsync()`
6. Return `EnhanceImportResult { ImportSessionHash, TotalTransactions, EnhancedCount, SkippedCount }`

---

## `Program.cs`

Register: `builder.Services.AddScoped<ITransactionEnhancer, TransactionEnhancer>()`

---

## Files

| Action | File |
|--------|------|
| Update | `Features/Transactions/TransactionTypes.cs` |
| New migration | `Infrastructure/Migrations/<timestamp>_AddImportSessionHashToTransaction` |
| New | `Features/Transactions/Import/Processing/EnhancedTransactionDescription.cs` |
| New | `Features/Transactions/Import/Processing/ITransactionEnhancer.cs` |
| New | `Features/Transactions/Import/Processing/TransactionEnhancer.cs` |
| New | `Features/Transactions/Import/TransactionEnhancementResult.cs` |
| New | `Features/Transactions/Import/EnhanceImportRequest.cs` |
| New | `Features/Transactions/Import/EnhanceImportResult.cs` |
| Update | `Features/Transactions/Import/ImportResult.cs` |
| Update | `Features/Transactions/Import/ImportApi.cs` |
| Update | `Program.cs` |
