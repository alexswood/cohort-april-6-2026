# CSV Import Implementation Plan

## Goal
Implement the CSV import feature for transaction data in the Budget Tracker API.

## Scope
- Add CsvHelper support for robust CSV parsing
- Create a flexible parser service that supports different bank CSV formats
- Wire the parser into the existing import endpoint
- Persist imported transactions to the database
- Add test coverage for import behavior and error handling

## Commit Plan

### Commit 1: Add package dependency
- Update `src/BudgetTracker.Api/BudgetTracker.Api.csproj`
- Add package reference for `CsvHelper` version `33.1.0`

### Commit 2: Implement CSV parsing service
- Add `src/BudgetTracker.Api/Features/Transactions/Import/Processing/CsvImporter.cs`
- Use `CsvHelper` to read CSV streams
- Support flexible header mapping for variations in bank CSV formats
- Parse culture-aware dates and amounts
- Validate required fields and capture row-level import errors
- Return an `ImportResult` and parsed `Transaction` objects

### Commit 3: Wire DI and endpoint
- Register `CsvImporter` in `src/BudgetTracker.Api/Program.cs`
- Update `src/BudgetTracker.Api/Features/Transactions/Import/ImportApi.cs`
  - Inject `CsvImporter`
  - Replace placeholder response with parsed CSV import logic
  - Persist parsed transactions to `BudgetTrackerContext`
  - Preserve file validation and error handling

### Commit 4: Verify and test
- Add or extend tests in `tests/BudgetTracker.Api.Tests/Features/Transactions/Import`
- Cover successful import response and persisted transaction data
- Cover invalid row handling and partial import errors
- Confirm existing file validation behavior remains correct

## Suggested Tests
### Parser unit tests (`CsvImporter`)
- `Should_parse_valid_csv_rows_into_transactions`
  - Valid CSV with `Date,Description,Amount,Balance`
  - Expect `ImportedCount` matches rows, `FailedCount == 0`
  - Expect parsed transactions have correct `Date`, `Description`, `Amount`, `Balance`, `Account`, `UserId`
- `Should_handle_alternate_header_names`
  - Valid CSV with alternate headers like `Transaction Date,Memo,Transaction Amount`
  - Expect parser succeeds via flexible header mapping
- `Should_report_invalid_rows_and_continue`
  - CSV containing one invalid row
  - Expect `TotalRows == n`, `ImportedCount == n-1`, `FailedCount == 1`
  - Expect errors include the failed row number and reason
- `Should_default_category_when_missing`
  - CSV without category column
  - Expect parsed transactions get a default category such as `Uncategorized`
- `Should_parse_common_currency_formats`
  - CSV with amount values like `$1,234.56`
  - Expect values parse correctly after cleanup

### Endpoint/integration tests
- `Should_import_csv_via_endpoint_and_persist_transactions`
  - POST `/api/transactions/import` with valid multipart CSV
  - Expect response `ImportedCount` equals row count
  - Expect transactions saved in DB for the user/account
- `Should_return_bad_request_for_invalid_file_input`
  - Verify missing file, wrong extension, oversized file, missing account
  - Matches existing validation behavior
- `Should_return_import_result_with_errors_for_bad_rows`
  - POST CSV with one invalid row
  - Expect `FailedCount == 1`, non-empty `Errors`, valid rows persisted

## Verification Steps
1. Build `src/BudgetTracker.Api`
2. Run import-related tests in `tests/BudgetTracker.Api.Tests`
3. Manually test `/api/transactions/import` using sample CSV files from `samples/`
4. Verify imported rows in the `Transactions` table

## Relevant Files
- `src/BudgetTracker.Api/BudgetTracker.Api.csproj`
- `src/BudgetTracker.Api/Features/Transactions/Import/Processing/CsvImporter.cs`
- `src/BudgetTracker.Api/Program.cs`
- `src/BudgetTracker.Api/Features/Transactions/Import/ImportApi.cs`
- `tests/BudgetTracker.Api.Tests/Features/Transactions/Import/ImportApiEndpointTests.cs`
- `tests/BudgetTracker.Api.Tests/Features/Transactions/Import/ImportResultTests.cs`
