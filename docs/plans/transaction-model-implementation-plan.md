# Plan: Implement Transaction Domain Model and Database Schema

Implement the core Transaction entity, DTO, database context updates, and migration for the budget tracking application's foundation, following exercise 001 specifications.

## Steps
1. Create `src/BudgetTracker.Api/Features/Transactions/TransactionTypes.cs` with the `Transaction` entity class including all required properties (Id, Date, Description, Amount, Balance, Category, Labels, ImportedAt, Account, UserId) and data annotations.
2. Add `TransactionDto` class and `TransactionExtensions.MapToDto` method to the same file for clean API responses.
3. Update `src/BudgetTracker.Api/Infrastructure/BudgetTrackerContext.cs` to add `DbSet<Transaction> Transactions` and configure indexes on Date, UserId, ImportedAt, plus foreign key relationship to ApplicationUser.
4. Generate and apply Entity Framework migration `AddTransactionEntity` to create the Transactions table in PostgreSQL.

## Relevant files
- `src/BudgetTracker.Api/Features/Transactions/TransactionTypes.cs` — New file for Transaction entity and DTO
- `src/BudgetTracker.Api/Infrastructure/BudgetTrackerContext.cs` — Update with Transactions DbSet and configuration

## Verification
1. Run `dotnet build` in `src/BudgetTracker.Api/` to ensure no compilation errors.
2. Run `dotnet ef database update` and verify the Transactions table exists in PostgreSQL.
3. Optionally, start the API with `dotnet run` and check Swagger for no errors.

## Decisions
- Follow exact specifications from 001-transactions.md without modifications.
- Use feature-based folder structure (`Features/Transactions/`) as established in the project guidelines.
- Include all specified indexes for query performance.
- Maintain multi-tenant support via UserId foreign key.

## Unit Tests Plan

### Step 1: Transaction Entity and DTO
**Test File:** `tests/BudgetTracker.Api.Tests/Features/Transactions/TransactionTypesTests.cs`

```csharp
[Fact]
public void Should_map_transaction_to_dto_correctly()
// Tests all properties map correctly

[Fact] 
public void Should_handle_null_values_in_mapping()
// Tests nullable properties handle null values
```

### Step 2: Database Context Configuration
**Test File:** `tests/BudgetTracker.Api.Tests/Infrastructure/BudgetTrackerContextTests.cs`

```csharp
[Fact]
public void Should_configure_transaction_entity_with_required_indexes()
// Verifies Date, UserId, ImportedAt indexes exist

[Fact]
public void Should_configure_foreign_key_relationship_to_user()
// Verifies foreign key to ApplicationUser
```

### Step 3: Migration Generation
**Not testable as a unit test** - Migrations are database schema changes verified through integration tests or manual inspection.