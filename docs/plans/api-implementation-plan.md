# Plan: Implement API Endpoints for Transactions

This plan follows the steps from `docs/tasks/002-api.md` and breaks the implementation into focused commits.

## Goal
Build working API endpoints for transaction management with authentication, pagination, import scaffolding, anti-forgery support, and test configuration.

## Commit Plan

### 1. Add pagination support and transaction list endpoint
- Create `src/BudgetTracker.Api/Features/Transactions/List/PagedResult.cs`.
- Create `src/BudgetTracker.Api/Features/Transactions/List/TransactionListApi.cs`.
- Implement the `GET /transactions` endpoint.
- Support `page` and `pageSize` query parameters.
- Validate `page` and `pageSize` values.
- Query only transactions belonging to the authenticated user.
- Sort by `Date` descending, then `ImportedAt` descending.
- Return a `PagedResult<Transaction>` response.

### 2. Add import result model and placeholder import endpoint
- Create `src/BudgetTracker.Api/Features/Transactions/Import/ImportResult.cs`.
- Create `src/BudgetTracker.Api/Features/Transactions/Import/ImportApi.cs`.
- Add `MapTransactionImportEndpoints` and `ImportAsync`.
- Implement file validation with `ValidateFileInput`.
- Return a placeholder `ImportResult` for now.
- Handle client errors and exceptions with `TypedResults.BadRequest`.

### 3. Add transaction API aggregator
- Create `src/BudgetTracker.Api/Features/Transactions/TransactionApi.cs`.
- Add grouped endpoint registration for `/transactions`.
- Apply `.WithTags("Transactions")`, `.WithOpenApi()`, and `.RequireAuthorization()`.
- Wire the list and import endpoints into the group.

### 4. Apply anti-forgery filter to import endpoint
- Update `ImportApi.cs` to call `.DisableAntiforgery()` on the POST `/import` route.
- Add `.AddEndpointFilter<ConditionalAntiforgeryFilter>()`.
- Include `using BudgetTracker.Api.AntiForgery;`.

### 5. Register endpoints in Program.cs
- Add `using BudgetTracker.Api.Features.Transactions;`.
- Call `app.MapGroup("/api").MapTransactionEndpoints();` before `app.Run();`.

### 6. Configure API key for testing
- Update `src/BudgetTracker.Api/appsettings.Development.json`.
- Add `StaticApiKeys.Keys.test-key-user1` with `UserId`, `Name`, and `Description`.
- Use the actual user ID from database or a development placeholder such as `admin@example.com`.

### 7. Add API testing artifact
- Create `test-api.http` at the repo root.
- Add requests for:
  - `GET http://localhost:5295/api/transactions`
  - `GET http://localhost:5295/api/transactions?page=1&pageSize=10`
  - `POST http://localhost:5295/api/transactions/import`
- Include example `X-API-Key: test-key-user1`.
- Use sample CSV from `samples/generic-bank-sample.csv`.

## Verification
1. Run the API from `src/BudgetTracker.Api/`.
2. Validate `GET /api/transactions` returns an empty paged result and correct metadata.
3. Validate `POST /api/transactions/import` returns placeholder `ImportResult`.
4. Confirm `X-API-Key` authorization works.
5. Confirm import endpoint uses conditional anti-forgery via `ConditionalAntiforgeryFilter`.
6. Execute the `.http` requests successfully in VS Code REST client or curl.

## Notes
- This exercise is intentionally limited to the endpoint scaffolding and placeholder import behavior.
- CSV parsing and storage logic belong to the next exercise (`003-csv-import.md`).

## Suggested Unit Tests

### Step 1: Pagination and transaction list endpoint
- `PagedResultTests`
  - `Should_calculate_total_pages_and_navigation_flags`
  - `Should_return_zero_total_pages_when_total_count_is_zero`
- `TransactionListApiTests`
  - `Should_return_transactions_for_authenticated_user_only`
  - `Should_apply_default_page_and_page_size_when_invalid_values_provided`
  - `Should_sort_transactions_by_date_then_importedAt_descending`
  - `Should_return_paged_result_with_total_count_page_and_pageSize`

Notes: Use an in-memory or SQLite test `BudgetTrackerContext`, seed transactions for multiple users, and assert only the authenticated user’s records are returned.

### Step 2: Import result model and placeholder endpoint
- `ImportResultTests`
  - `Should_initialize_errors_list`
  - `Should_store_source_file_and_imported_at`
- `ImportApiTests`
  - `Should_return_bad_request_when_file_is_missing`
  - `Should_return_bad_request_for_non_csv_file`
  - `Should_return_bad_request_when_account_is_empty`
  - `Should_return_placeholder_import_result_for_valid_input`

Notes: Mock `IFormFile` with a memory stream and validate `ValidateFileInput` separately from `ImportAsync`.

### Step 3: Transaction API aggregator
- `TransactionApiTests`
  - `Should_register_transaction_endpoints_without_throwing`

Notes: This is mostly a wiring test; verify the extension method can be called with a fake route builder, or use a startup test.

### Step 4: Anti-forgery filter on import endpoint
- `ImportApiEndpointFilterTests`
  - `Should_apply_disableAntiforgery_and_conditional_filter_to_import_route`

Notes: This is best verified by inspecting endpoint metadata after app startup or through integration testing.

### Step 5: Program.cs registration
- No direct unit test needed; verify via runtime route availability.

### Step 6: API key configuration
- `ConfigurationTests` (optional)
  - `Should_load_test_api_key_configuration_from_appsettings`
  - `Should_contain_test_key_user1`

Notes: Use configuration binding tests to validate JSON shape.

### Step 7: `test-api.http`
- Not unit-testable.
- Verify manually with REST client or curl once the API is running.
