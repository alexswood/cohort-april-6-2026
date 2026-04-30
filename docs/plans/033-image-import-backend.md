# Plan: Task 033 — Multimodal Image Import Backend

## Current State Assessment

| Step | Status | Notes |
|------|--------|-------|
| 33.1 — Create `IImageImporter` | **TODO** | File doesn't exist |
| 33.2 — Verify `StringExtensions` | **DONE** | File exists with `ExtractJsonFromCodeBlock` |
| 33.3 — Update `TransactionEnhancer` | **DONE** | Already uses `StringExtensions` |
| 33.4 — Implement `ImageImporter` | **TODO** | File doesn't exist |
| 33.5 — Register in DI | **TODO** | Only `CsvImporter` registered |
| 33.6 — Update `ImportApi` | **TODO** | Only accepts `.csv` files |

---

## Step 1 — Create `IImageImporter.cs`

Create `src/BudgetTracker.Api/Features/Transactions/Import/Processing/IImageImporter.cs` exactly as specified in the task — no changes needed from the spec.

---

## Step 2 — Create `ImageImporter.cs`

Create `src/BudgetTracker.Api/Features/Transactions/Import/Processing/ImageImporter.cs` as specified. Key implementation points:

- Uses `IChatClient` (already registered in DI for the AI features)
- Converts image bytes to base64 via `DataContent` for the multimodal message
- Media type dispatch: `.png` → `image/png`, `.jpg`/`.jpeg` → `image/jpeg`
- Parses `confidence_score` from JSON — logs a warning and adds an `Errors` entry if below `0.7`
- Uses `ExtractJsonFromCodeBlock()` from `StringExtensions` to handle AI responses wrapped in code fences
- Required fields per transaction: `date`, `description`, `amount` — `balance` is optional

---

## Step 3 — Register `IImageImporter` in `Program.cs`

Add one line after the existing `AddScoped<CsvImporter>()` registration in `src/BudgetTracker.Api/Program.cs`:

```csharp
builder.Services.AddScoped<IImageImporter, ImageImporter>();
```

---

## Step 4 — Refactor `ImportApi.cs`

This is the most involved change. The current `ImportAsync` in
`src/BudgetTracker.Api/Features/Transactions/Import/ImportApi.cs` hardcodes the CSV-only path.

**4a. Add `IImageImporter` parameter** to `ImportAsync`.

**4b. Update `ValidateFileInput`** — currently rejects anything without `.csv`. Change to accept
`.csv`, `.png`, `.jpg`, `.jpeg` and update the error message accordingly. Signature stays
`(IFormFile file, string account)`.

**4c. Extract CSV logic into `ProcessCsvFileAsync`** — moves the detection call, the position reset,
and `ParseCsvAsync` call here. Returns `(ImportResult, List<Transaction>, CsvStructureDetectionResult?)`.
If detection confidence is below 85, returns the empty result + detection result (so the caller can
issue the `BadRequest`).

**4d. Add `ProcessImageFileAsync`** — calls `imageImporter.ProcessImageAsync(...)` and returns
`(ImportResult, List<Transaction>, null)` (no detection result for images).

**4e. Add `ProcessFileAsync` dispatcher** — switches on file extension:
- `.csv` → `ProcessCsvFileAsync`
- `.png` / `.jpg` / `.jpeg` → `ProcessImageFileAsync`
- anything else → throws `InvalidOperationException` (already blocked by validation, but belt-and-suspenders)

**4f. Reshape `ImportAsync` body**:

```
validate
  → ProcessFileAsync
  → if CSV and low confidence → BadRequest
  → set detection metadata on result
  → early return if no transactions
  → session hash
  → enhancement pipeline
  → save to DB
  → Ok
```

The enhancement pipeline and DB save are **shared** between CSV and image paths — image transactions
go through the same `EnhanceDescriptionsAsync` + `context.SaveChangesAsync` flow.

---

## Key Design Decisions

- `using var stream` stays (not `await using`) to match the existing pattern.
- The detection confidence check remains in `ImportAsync` (not buried in `ProcessCsvFileAsync`) — the
  method returns `CsvStructureDetectionResult?` so the caller can produce the correct `BadRequest`
  message with `DetectionMethod` context.
- Image results have `DetectionMethod = null` and `DetectionConfidence = 0` on the returned
  `ImportResult` — no special-casing needed downstream.
- No changes to the `/import/enhance` endpoint or `EnhanceAsync` method.

---

## Files Touched

| File | Action |
|------|--------|
| `src/BudgetTracker.Api/Features/Transactions/Import/Processing/IImageImporter.cs` | Create |
| `src/BudgetTracker.Api/Features/Transactions/Import/Processing/ImageImporter.cs` | Create |
| `src/BudgetTracker.Api/Program.cs` | Add one DI registration line |
| `src/BudgetTracker.Api/Features/Transactions/Import/ImportApi.cs` | Refactor — add image routing, update validation |
