# BudgetTracker.Api — Pending Enhancements

## Medium Impact

### 1. `ImportAsync` handler doing too much
`ImportApi.cs` lines 28–107 — a single handler does validation, CSV detection, parsing, DB save, and AI enhancement matching (~80 lines). Extracting a small `ImportService` would make the route handler a short orchestrator and the logic independently testable.

### 2. Exception messages exposed to clients
`ImportApi.cs` lines 105 and 165 return raw `ex.Message` in `BadRequest`. Replace with a generic user-facing message and log the original exception server-side.

### 3. Unsafe `!` in `GetUserId`
`Auth/ClaimsPrincipalExtensions.cs` line 8 uses the null-forgiving operator on the claim value. Returning `string?` and handling null explicitly at call sites makes the failure mode visible rather than a surprise `NullReferenceException`.

### 4. CSV delimiter detection ignored on simple parsing path
`CsvStructureDetector.cs` detects the delimiter and stores it in `CsvStructureDetectionResult`, but the simple (non-AI) parsing path hardcodes comma splitting instead of using the detected value. `CsvImporter.cs` only respects the delimiter when falling back to AI detection.

---

## Low Impact / Housekeeping

### 5. Redundant two-param overload in `CsvImporter`
`CsvImporter.cs` lines 13–16 — a two-param overload that just calls the five-param version with `null`. Remove it and update the one call site to pass the arguments directly.

### 6. Duplicated culture-parsing logic
`CsvImporter.cs` lines 173–234 — `TryParseDate` and `TryParseAmountWithCulture` both build the same culture fallback array independently. Extract a shared helper to remove the duplication.

### 7. Hardcoded magic numbers
Several thresholds and limits are scattered across files with no named constant:
- 85% confidence threshold — `ImportApi.cs` line 51, `CsvStructureDetector.cs` line 26
- 10 MB file size limit — `ImportApi.cs` line 188
- Default page size 20 — `TransactionListApi.cs` line 16
- First 100 lines sampled for simple detection — `CsvStructureDetector.cs` line 53
- First 5 lines sampled for AI detection — `CsvDetector.cs` line 31
