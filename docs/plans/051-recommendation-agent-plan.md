# Plan: Recommendation Agent Backend (Task 051)

## Context

Building the backend infrastructure for an autonomous AI-powered recommendation system. The existing codebase has:

- `Features/Intelligence/Query/` with `QueryApi.cs` exposing `MapQueryEndpoints()`
- `Features/Intelligence/Search/` with semantic search services
- `Infrastructure/BudgetTrackerContext.cs` with only a `Transactions` DbSet
- `Program.cs` endpoint chain ending with `.MapQueryEndpoints()`
- `IChatClient` (Azure OpenAI) already registered as Singleton
- `StringExtensions.ExtractJsonFromCodeBlock()` already available
- `ClaimsPrincipalExtensions.GetUserId()` already available
- `docs/plans/` exists; `Features/Intelligence/IntelligenceEndpoints.cs` does NOT exist yet

This step adds: data model, repository interface, recommendation agent, background service, worker, API endpoint, intelligence endpoints aggregator, DB context update, service registrations, and migration.

---

## Files to Create

### 1. Data Model
**`src/BudgetTracker.Api/Features/Intelligence/Recommendations/Recommendation.cs`**
- `Recommendation` entity: `Id` (Guid), `UserId`, `Title` (max 200), `Message` (max 1000), `Type`, `Priority`, `GeneratedAt` (timestamptz), `ExpiresAt` (timestamptz), `Status` (default Active)
- `RecommendationType` enum (JsonStringEnumConverter): `SpendingAlert`, `SavingsOpportunity`, `BehavioralInsight`, `BudgetWarning`
- `RecommendationPriority` enum (JsonStringEnumConverter): `Low=1`, `Medium=2`, `High=3`, `Critical=4`
- `RecommendationStatus` enum: `Active`, `Expired`
- `RecommendationDto` (projection — no UserId/Status)
- Internal `RecommendationExtensions` with `MapToDto()` extension method

### 2. Repository Interface
**`src/BudgetTracker.Api/Features/Intelligence/Recommendations/IRecommendationRepository.cs`**
- `IRecommendationRepository`: `GetActiveRecommendationsAsync(string userId)` → `Task<List<Recommendation>>`, `GenerateRecommendationsAsync(string userId)` → `Task`
- `IRecommendationWorker`: `ProcessAllUsersRecommendationsAsync()`, `ProcessUserRecommendationsAsync(string userId)`

### 3. Recommendation Agent
**`src/BudgetTracker.Api/Features/Intelligence/Recommendations/RecommendationAgent.cs`**

Implements `IRecommendationRepository`. Injects `BudgetTrackerContext`, `IChatClient`, `ILogger<RecommendationAgent>`.

`GetActiveRecommendationsAsync`: queries `_context.Recommendations` filtered by `UserId`, `Status == Active`, `ExpiresAt > UtcNow`, ordered by Priority desc then GeneratedAt desc, takes 5.

`GenerateRecommendationsAsync` flow:
1. Check `lastGenerated` (max GeneratedAt for user) vs `lastImported` (max ImportedAt for user's transactions) — skip if generated is more recent (with 1-minute dev buffer)
2. Guard: fewer than 5 transactions → log and return
3. Gather `BasicStats` (income, expenses, date range, top 5 expense categories)
4. Call AI with a system prompt requesting JSON `{ recommendations: [...] }` and a user prompt with the stats
5. Parse JSON response via `ExtractJsonFromCodeBlock()` + `JsonSerializer`
6. Store: expire existing Active recommendations, insert new ones with 7-day expiry

Internal types: `GeneratedRecommendation` (Title, Message, Type, Priority), `BasicStats` (TransactionCount, TotalIncome, TotalExpenses, DateRange, TopCategories)

### 4. Background Service
**`src/BudgetTracker.Api/Features/Intelligence/Recommendations/RecommendationBackgroundService.cs`**

Extends `BackgroundService`. Injects `IServiceScopeFactory`, `ILogger`.

`ExecuteAsync`:
- Waits 30 seconds on startup
- Loop: resolve `IRecommendationWorker` from scope, call `ProcessAllUsersRecommendationsAsync()`, run inline cleanup, then sleep until next 6 AM UTC (or 1 hour if calculation is stale)
- Cleanup: marks Active recommendations with `ExpiresAt <= UtcNow` as Expired; hard-deletes recommendations older than 30 days

`GetNextRunTime`: returns today at 06:00 UTC if not yet passed, else tomorrow at 06:00 UTC.

### 5. Recommendation Worker
**`src/BudgetTracker.Api/Features/Intelligence/Recommendations/RecommendationProcessor.cs`**

Implements `IRecommendationWorker`. Injects `BudgetTrackerContext`, `IRecommendationRepository`, `ILogger`.

`ProcessAllUsersRecommendationsAsync`: gets distinct `UserId`s from Transactions, iterates with a 100 ms delay between users, calls `ProcessUserRecommendationsAsync` for each, logs totals.

`ProcessUserRecommendationsAsync`: delegates to `_repository.GenerateRecommendationsAsync(userId)`.

### 6. API Endpoint
**`src/BudgetTracker.Api/Features/Intelligence/Recommendations/RecommendationApi.cs`**

Static class with `MapRecommendationEndpoints(this IEndpointRouteBuilder routes)`:
- `GET /recommendations` — requires authorization, resolves userId from `ClaimsPrincipal`, calls `GetActiveRecommendationsAsync`, returns `200 OK` with `List<RecommendationDto>`

### 7. Intelligence Endpoints Aggregator
**`src/BudgetTracker.Api/Features/Intelligence/IntelligenceEndpoints.cs`**

Static class with `MapIntelligenceEndpoints(this IEndpointRouteBuilder endpoints)`:
- Calls `endpoints.MapQueryEndpoints()` then `endpoints.MapRecommendationEndpoints()`

---

## Files to Update

### `src/BudgetTracker.Api/Infrastructure/BudgetTrackerContext.cs`
- Add using: `using BudgetTracker.Api.Features.Intelligence.Recommendations;`
- Add DbSet: `public DbSet<Recommendation> Recommendations { get; set; }`

### `src/BudgetTracker.Api/Program.cs`
- Add usings:
  ```csharp
  using BudgetTracker.Api.Features.Intelligence;
  using BudgetTracker.Api.Features.Intelligence.Recommendations;
  ```
- Add service registrations (after existing AI service registrations):
  ```csharp
  builder.Services.AddScoped<IRecommendationRepository, RecommendationAgent>();
  builder.Services.AddScoped<IRecommendationWorker, RecommendationProcessor>();
  builder.Services.AddHostedService<RecommendationBackgroundService>();
  ```
- Replace `.MapQueryEndpoints()` in the endpoint chain with `.MapIntelligenceEndpoints()`

---

## Database Migration

From `src/BudgetTracker.Api/`:
```bash
dotnet ef migrations add AddRecommendations
dotnet ef database update
```

Creates a `Recommendations` table with columns: `Id` (uuid PK), `UserId` (text), `Title` (varchar 200), `Message` (varchar 1000), `Type` (text), `Priority` (int), `GeneratedAt` (timestamptz), `ExpiresAt` (timestamptz), `Status` (text).

---

## Dependency / Build Notes

- `RecommendationBackgroundService` uses `IServiceScopeFactory` to avoid scoped-in-singleton issues — `IRecommendationWorker` and `BudgetTrackerContext` must remain Scoped.
- `RecommendationAgent` is Scoped; `IChatClient` is Singleton — injection is safe.
- `GetResponseAsync` is from `Microsoft.Extensions.AI` (already used in `QueryAssistantService`) — no new package needed.
- `ExtractJsonFromCodeBlock` is in `BudgetTracker.Api.Infrastructure.Extensions` — add the using to `RecommendationAgent.cs`.

---

## Execution Order

1. Create `Recommendation.cs` (data model — no deps)
2. Create `IRecommendationRepository.cs` (interface — depends on model)
3. Create `RecommendationAgent.cs` (implements interface — depends on model + EF + AI)
4. Create `RecommendationProcessor.cs` (depends on interface)
5. Create `RecommendationBackgroundService.cs` (depends on `IRecommendationWorker`)
6. Create `RecommendationApi.cs` (depends on interface + DTO)
7. Create `IntelligenceEndpoints.cs` (depends on QueryApi + RecommendationApi)
8. Update `BudgetTrackerContext.cs` (add DbSet)
9. Update `Program.cs` (registrations + endpoint chain)
10. Run EF migration
11. Test: start API, check logs for background service startup, hit `GET /api/recommendations`
