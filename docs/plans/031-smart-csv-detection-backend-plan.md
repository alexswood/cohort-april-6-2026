# Plan: 031 – Smart CSV Detection Backend

## Goal

Add intelligent, layered CSV structure detection to the import pipeline. The system tries fast rule-based detection first; if confidence is low it falls back to AI analysis. The result drives culture-aware and column-mapping-aware parsing in `CsvImporter`.

---

## Existing Code Inventory

| File | Status | Notes |
|---|---|---|
| `Import/ImportResult.cs` | Modify | Add `DetectionMethod` and `DetectionConfidence` properties |
| `Import/ImportApi.cs` | Modify | Inject `ICsvStructureDetector`; call detection before parse |
| `Import/Processing/CsvImporter.cs` | Rewrite | Accept `CsvStructureDetectionResult?`; use culture-aware parse |
| `Program.cs` | Modify | Register 3 new scoped services |
| `Import/Enhancement/TransactionEnhancer.cs` | Optional refactor | Has its own private `ExtractJsonFromCodeBlock` – can be updated to use the new `StringExtensions` method after the shared utility is created |

---

## Files to Create

```
src/BudgetTracker.Api/
├── Features/Transactions/Import/Detection/
│   ├── CsvStructureDetectionResult.cs   (Step 31.1)
│   ├── ColumnMappingDictionary.cs       (Step 31.2)
│   ├── ICsvStructureDetector.cs         (Step 31.3)
│   ├── ICsvDetector.cs                  (Step 31.6)
│   ├── CsvDetector.cs                   (Step 31.6)
│   └── CsvStructureDetector.cs          (Step 31.7)
├── Features/Transactions/Import/Processing/
│   ├── ICsvAnalyzer.cs                  (Step 31.4)
│   └── CsvAnalyzer.cs                   (Step 31.4)
└── Infrastructure/Extensions/
    └── StringExtensions.cs              (Step 31.5)
```

---

## Implementation Steps

### Step 1 – Detection result types
**File:** `Import/Detection/CsvStructureDetectionResult.cs`

Create `CsvStructureDetectionResult` with:
- `Delimiter` (default `","`)
- `ColumnMappings` (`Dictionary<string, string>`) – keys are canonical names (`"Date"`, `"Description"`, `"Amount"`, `"Balance"`, `"Category"`), values are actual CSV column headers
- `CultureCode` (default `"en-US"`)
- `ConfidenceScore` (`double`, 0–100)
- `DetectionMethod` (`DetectionMethod` enum)

Create `DetectionMethod` enum: `RuleBased`, `AI`.

---

### Step 2 – Column mapping dictionary
**File:** `Import/Detection/ColumnMappingDictionary.cs`

Static class with three `string[]` arrays of common English column headers:
- `DateColumns` – `["Date", "Transaction Date", "Posting Date", "Value Date", "Txn Date"]`
- `DescriptionColumns` – `["Description", "Memo", "Details", "Transaction Description", "Reference"]`
- `AmountColumns` – `["Amount", "Transaction Amount", "Debit", "Credit", "Value"]`

Used only by the rule-based path; AI handles non-English headers directly.

---

### Step 3 – `ICsvStructureDetector` interface
**File:** `Import/Detection/ICsvStructureDetector.cs`

Single method: `Task<CsvStructureDetectionResult> DetectStructureAsync(Stream csvStream)`.

This is the public entry point injected into `ImportApi`.

---

### Step 4 – AI analyzer (`ICsvAnalyzer` / `CsvAnalyzer`)
**Files:** `Import/Processing/ICsvAnalyzer.cs` and `CsvAnalyzer.cs`

`CsvAnalyzer` wraps `IChatClient`. Its `AnalyzeCsvStructureAsync(string csvContent)` method:
1. Builds a system prompt ("CSV structure analysis expert") and a detailed user prompt asking the model to return a JSON object with `columnSeparator`, `cultureCode`, `dateColumn`, `dateFormat`, `descriptionColumn`, `amountColumn`, and `confidenceScore`.
2. Calls `_chatClient.GetResponseAsync(...)` and returns the raw text.

No JSON parsing here – that's `CsvDetector`'s job.

---

### Step 5 – JSON extraction utility
**File:** `Infrastructure/Extensions/StringExtensions.cs`

Extension method `ExtractJsonFromCodeBlock(this string input)`:
- If the string doesn't contain ` ```json `, return it as-is.
- Otherwise, extract content between ` ```json ` and ` ``` ` via regex.
- Throw `FormatException` if the regex doesn't match.

**Note:** `TransactionEnhancer` already has a private version of this logic. After this file is created, optionally update `TransactionEnhancer` to call the extension method instead (keeping it private is also fine since refactoring it is not required by the task).

---

### Step 6 – AI detection service (`ICsvDetector` / `CsvDetector`)
**Files:** `Import/Detection/ICsvDetector.cs` and `CsvDetector.cs`

`CsvDetector` bridges raw AI text and structured results:
1. Reads the first 5 non-empty lines from the stream.
2. Calls `ICsvAnalyzer.AnalyzeCsvStructureAsync(csvContent)`.
3. Calls `ExtractJsonFromCodeBlock()` on the response.
4. Parses the JSON with `JsonDocument` to populate `CsvStructureDetectionResult` (delimiter, culture, column mappings, confidence score).
5. Returns a zero-confidence result on any failure.

`ICsvDetector` method: `Task<CsvStructureDetectionResult> AnalyzeCsvStructureAsync(Stream csvStream)`.

---

### Step 7 – Orchestrator (`CsvStructureDetector`)
**File:** `Import/Detection/CsvStructureDetector.cs`

Implements `ICsvStructureDetector`. Orchestration logic:

```
DetectStructureAsync(stream):
  simpleResult = TrySimpleParsing(stream)
  if simpleResult.ConfidenceScore >= 85 → return simpleResult
  reset stream.Position = 0
  return await _aiDetectionService.AnalyzeCsvStructureAsync(stream)
```

`TrySimpleParsing`:
1. Reads up to 100 lines; fails fast if none.
2. Splits header on `','` and trims quotes.
3. Uses `ColumnMappingDictionary` to find Date, Description, Amount columns (case-insensitive).
4. Returns confidence 0 if any required column is missing.
5. Validates up to 3 data rows: parses date with `InvariantCulture`, parses amount after stripping currency symbols.
6. Confidence = `(successfulParses / totalSamples) * 100`; if no data rows, returns 85.

---

### Step 8 – Update `CsvImporter`
**File:** `Import/Processing/CsvImporter.cs` – full replacement

Changes:
- Add overload `ParseCsvAsync(..., CsvStructureDetectionResult? detectionResult)`.
- Original no-arg signature delegates to the new overload with `null`.
- `CsvReader` picks up `detectionResult?.Delimiter ?? ","`.
- New `GetColumnValueWithDetection` helper: tries the detected column name first, then falls back to English defaults.
- `TryParseDate` uses `detectionResult.CultureCode` when set.
- `TryParseAmountWithCulture` uses `detectionResult.CultureCode` for decimal/thousands separator handling.
- Remove the old `TryParseAmount` method.

Imports to add: `using BudgetTracker.Api.Features.Transactions.Import.Detection;`

---

### Step 9 – Update `ImportResult`
**File:** `Import/ImportResult.cs`

Add two properties:
```csharp
public string? DetectionMethod { get; set; }
public double DetectionConfidence { get; set; }
```

---

### Step 10 – Update `ImportApi`
**File:** `Import/ImportApi.cs`

Changes to `ImportAsync`:
1. Add `ICsvStructureDetector detectionService` parameter.
2. Change `using var stream` to `await using var stream` (file stream is `IAsyncDisposable`-compatible; if not, keep `using` but ensure stream stays open during both calls).
3. Call `detectionService.DetectStructureAsync(stream)` before parsing.
4. If `detectionResult.ConfidenceScore < 85`, return `BadRequest` with a descriptive message (AI-specific vs generic).
5. Reset `stream.Position = 0` before passing to `csvImporter.ParseCsvAsync`.
6. Pass `detectionResult` to `ParseCsvAsync`.
7. Set `importResult.DetectionMethod` and `importResult.DetectionConfidence` from the detection result.

Imports to add:
```csharp
using BudgetTracker.Api.Features.Transactions.Import.Detection;
```

---

### Step 11 – Register services in `Program.cs`

```csharp
builder.Services.AddScoped<ICsvStructureDetector, CsvStructureDetector>();
builder.Services.AddScoped<ICsvDetector, CsvDetector>();
builder.Services.AddScoped<ICsvAnalyzer, CsvAnalyzer>();
```

Place these near the existing `CsvImporter` or AI-related registrations.

---

## Dependency Graph

```
ImportApi
  └─ ICsvStructureDetector (CsvStructureDetector)
       ├─ Rule-based path (no extra dependencies)
       └─ ICsvDetector (CsvDetector)
            └─ ICsvAnalyzer (CsvAnalyzer)
                 └─ IChatClient (already registered)
```

---

## Key Design Decisions

| Decision | Rationale |
|---|---|
| Confidence threshold of 85 | Matches both rule-based validation success rate and AI minimum confidence |
| Stream reset between detection and parsing | `OpenReadStream()` returns a seekable stream; resetting with `stream.Position = 0` is safe |
| Rule-based only handles English, comma-delimited CSVs | Keeps the fast path simple; any other format falls to AI |
| Zero-confidence on AI failure | Safe default; surfaces the error via `BadRequest` rather than silently producing bad data |
| `CsvAnalyzer` returns raw text | Keeps AI communication separate from JSON parsing; easier to test each in isolation |

---

## Testing Scenarios

### Rule-Based (happy path)
```csv
Date,Description,Amount,Balance
2025-01-15,STARBUCKS COFFEE #1234,-4.50,1245.50
```
Expected: `DetectionMethod = RuleBased`, confidence ≥ 85, no AI call.

### AI Detection (Portuguese bank format)
```csv
Data;Descrição;Valor;Saldo
15/01/2025;COMPRA CONTINENTE;-45,67;1.234,56
```
Expected: `DetectionMethod = AI`, `CultureCode = pt-PT`, `Delimiter = ";"`.

### Confidence too low
A CSV with unrecognisable headers and a failed AI response should return `400 Bad Request` with an explanation.
