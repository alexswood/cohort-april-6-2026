# Plan: Natural Language Query Assistant Backend (Task 042)

## Context

Building on the RAG infrastructure from task 041. The existing codebase already has:
- `IChatClient` registered as Singleton (Azure OpenAI)
- `IAzureEmbeddingService` registered as Scoped
- `EmbeddingBackgroundService` generating embeddings
- `Transactions` table with a `Vector`-typed `Embedding` column
- All endpoints currently mapped under `/api` group in `Program.cs`

---

## Files to Create

### Part 1 — Semantic Search Service

**`src/BudgetTracker.Api/Features/Intelligence/Search/ISemanticSearchService.cs`**
- Single method: `FindRelevantTransactionsAsync(string queryText, string userId, int maxResults = 50)`
- Returns `Task<List<Transaction>>`

**`src/BudgetTracker.Api/Features/Intelligence/Search/SemanticSearchService.cs`**
- Injects `BudgetTrackerContext`, `IAzureEmbeddingService`, `ILogger`
- Converts the query string to a vector via `_embeddingService.GenerateEmbeddingAsync()`
- Runs a raw SQL query using `cosine_distance("Embedding", {vector}::vector)` ordered ASC, filtered by `UserId`, limited to `maxResults`
- Returns empty list on any error (graceful degradation)

### Part 2 — Query Assistant Service

**`src/BudgetTracker.Api/Features/Intelligence/Query/IQueryAssistantService.cs`**
- Interface + `QueryRequest` (string `Query`) + `QueryResponse` (string `Answer`, decimal? `Amount`, `List<TransactionDto>?` `Transactions`)

**`src/BudgetTracker.Api/Features/Intelligence/Query/QueryAssistantService.cs`**
- Injects `BudgetTrackerContext`, `ISemanticSearchService`, `IChatClient`, `ILogger`
- `ProcessQueryAsync` flow:
  1. Guard: empty/long query or no userId → return early with message
  2. Guard: user has no transactions → return early with prompt to import
  3. Run semantic search (top 10 relevant transactions)
  4. Load 10 most recent transactions for summary context
  5. Build system prompt (financial assistant persona, JSON-output contract)
  6. Build user prompt (summary stats + category breakdown + recent + relevant transactions)
  7. Call `_chatClient.GetResponseAsync(...)` via `Microsoft.Extensions.AI`
  8. Parse JSON response → `QueryResponse` with matched `TransactionDto` list
- Private inner classes `AiQueryResponse` and `AiTransactionReference` for JSON deserialization

### Part 3 — API Endpoint

**`src/BudgetTracker.Api/Features/Intelligence/Query/QueryApi.cs`**
- Extension method `MapQueryEndpoints(this IEndpointRouteBuilder routes)`
- Maps `POST /query/ask` requiring authorization
- Extracts `userId` from `ClaimsPrincipal`, delegates to `IQueryAssistantService`
- Returns `200 OK` with `QueryResponse`

---

## Files to Update

**`src/BudgetTracker.Api/Program.cs`** — two changes:
- **Service registration** (near existing AI services around line 101):
  ```csharp
  builder.Services.AddScoped<ISemanticSearchService, SemanticSearchService>();
  builder.Services.AddScoped<IQueryAssistantService, QueryAssistantService>();
  ```
- **Endpoint mapping** (inside the `/api` group):
  ```csharp
  .MapQueryEndpoints()
  ```

---

## Key Dependencies Already in Place

| Dependency | Status |
|---|---|
| `IChatClient` | Registered as Singleton |
| `IAzureEmbeddingService` | Registered as Scoped |
| `pgvector` / `cosine_distance` SQL | Working (used in existing code) |
| `TransactionDto` + `MapToDto()` | Exists in Transactions feature |
| `ClaimsPrincipal.GetUserId()` | Exists in Auth feature |

---

## Execution Order

### ✅ Completed

1. ✅ Create `ISemanticSearchService` → `SemanticSearchService`
   - File: `src/BudgetTracker.Api/Features/Intelligence/Search/ISemanticSearchService.cs`
   - File: `src/BudgetTracker.Api/Features/Intelligence/Search/SemanticSearchService.cs`
   
2. ✅ Create `IQueryAssistantService` types → `QueryAssistantService`
   - File: `src/BudgetTracker.Api/Features/Intelligence/Query/IQueryAssistantService.cs`
   - File: `src/BudgetTracker.Api/Features/Intelligence/Query/QueryAssistantService.cs`
   
3. ✅ Create `QueryApi` endpoints
   - File: `src/BudgetTracker.Api/Features/Intelligence/Query/QueryApi.cs`
   
4. ✅ Update `Program.cs`
   - Added import for `BudgetTracker.Api.Features.Intelligence.Query`
   - Registered `ISemanticSearchService, SemanticSearchService`
   - Registered `IQueryAssistantService, QueryAssistantService`
   - Added `.MapQueryEndpoints()` to endpoint mapping
   - Build succeeded with 0 warnings, 0 errors

### 🔄 Remaining

5. Start API server and test with HTTP samples:
   - Test coffee-related query
   - Test expense analysis
   - Test category analysis
   - Verify semantic matching works
   - Verify AI responses are properly formatted
