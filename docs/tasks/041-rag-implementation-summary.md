# RAG Enhancement Implementation - Completed Steps

## Summary
Successfully implemented Retrieval Augmented Generation (RAG) for transaction categorization. The backend is ready for testing.

---

## Completed Implementation

### Part 1: Vector Database Infrastructure ✅
- [x] Installed `Pgvector.EntityFrameworkCore` NuGet package (v0.2.2)
- [x] Added `Vector? Embedding` property to Transaction entity
- [x] Configured BudgetTrackerContext with:
  - pgvector extension enabled
  - Vector column type: `vector(1536)`
  - RAG context composite index: `(UserId, Account, Date)` with date descending
  - Category index with non-null filter
  - Vector index with HNSW method and cosine operators
- [x] Created database migration: `AddVectorEmbeddings`

**Status**: Ready - migration created, awaiting database reset and application

### Part 2: Embedding Services ✅
- [x] Created `IAzureEmbeddingService` interface with methods:
  - `GenerateEmbeddingAsync(string text)`
  - `GenerateTransactionEmbeddingAsync(string description, string? category)`
- [x] Implemented `AzureEmbeddingService` using `IEmbeddingGenerator<string, Embedding<float>>`
- [x] Created `EmbeddingBackgroundService` with:
  - 5-minute processing interval
  - Batch size: 50 transactions
  - Filters: last 24 hours, no existing embedding, with category
  - Proper logging and error handling

**Files Created**:
- `src/BudgetTracker.Api/Features/Intelligence/Search/IAzureEmbeddingService.cs`
- `src/BudgetTracker.Api/Features/Intelligence/Search/AzureEmbeddingService.cs`
- `src/BudgetTracker.Api/Features/Intelligence/Search/EmbeddingBackgroundService.cs`

### Part 3: RAG Integration ✅
- [x] Updated `TransactionEnhancer` with:
  - RAG configuration constants: `DefaultContextLimit = 25`, `ContextWindowDays = 365`
  - `GetSemanticContextTransactionsAsync` method for semantic similarity retrieval
  - `CreateEnhancedSystemPrompt` that includes context transactions
  - `EnhanceDescriptionsAsync` signature updated with `currentImportSessionHash` parameter
- [x] Updated `ITransactionEnhancer` interface with new method signature
- [x] Updated `ImportApi.cs` to pass `sessionHash` parameter

**Key Features**:
- Cosine distance threshold: 0.6 for semantic similarity
- Retrieves semantically similar transactions from last 365 days
- Combines similar merchant patterns with AI enhancement
- Falls back gracefully if embedding service fails

### Part 4: Dependency Injection ✅
- [x] Added service registrations in `Program.cs`:
  - `IEmbeddingGenerator<string, Embedding<float>>` singleton
  - `IAzureEmbeddingService` scoped
  - `EmbeddingBackgroundService` hosted service
  - Updated DbContext to use `.UseVector()`
- [x] Added `EmbeddingDeploymentName` to `AzureAiConfiguration`
- [x] Build successful with no errors

---

## Next Steps: Configuration & Testing

### 1. Deploy Text Embedding Model in Azure AI Foundry
1. Go to https://ai.azure.com/
2. Navigate to **Deployments** → **Deploy model**
3. Search for and select **"text-embedding-3-small"**
4. Set deployment name to **"text-embedding-3-small"**
5. Click "Deploy" (wait 5-10 minutes for completion)

### 2. Configure Embedding Deployment in User Secrets
```bash
cd src/BudgetTracker.Api
dotnet user-secrets set "AzureAi:EmbeddingDeploymentName" "text-embedding-3-small"
```

Verify configuration:
```bash
dotnet user-secrets list
```

Should include:
```
AzureAi:EmbeddingDeploymentName = text-embedding-3-small
AzureAi:DeploymentName = gpt-4.1-mini
AzureAi:Endpoint = https://your-resource.openai.azure.com/
AzureAi:ApiKey = your-api-key
```

### 3. Reset Database and Create New User Account

**⚠️ WARNING**: This will delete all existing data. Only do this if starting fresh.

The `docker-compose.yml` is already configured with `pgvector/pgvector:pg17` image which includes pgvector support.

```bash
# Stop and remove containers/volumes
docker compose down -v

# Start fresh with pgvector-enabled PostgreSQL
# (The image in docker-compose.yml is already set to pgvector/pgvector:pg17)
docker compose up -d

# Start API (from src/BudgetTracker.Api/)
dotnet run

# In another terminal, start Web (from src/BudgetTracker.Web/)
npm run dev
```

Register new account at `http://localhost:5173/register`:
- Email: `test@example.com` (or any email)
- Password: Must meet requirements (e.g., `P@ssw0rd`)

### 4. Get Your User ID

1. Open browser DevTools (F12)
2. Go to **Network** tab
3. Reload the page
4. Find request to `/me`
5. Check **Response** tab for `userId`

### 5. Update API Configuration

Update `appsettings.Development.json`:
```json
{
  "StaticApiKeys": {
    "Keys": {
      "test-key-user1": {
        "UserId": "<your-new-user-id>",
        "Name": "Test User",
        "Description": "API key for cohort testing"
      }
    }
  }
}
```

Restart API for changes to take effect.

### 6. Apply Database Migration

```bash
cd src/BudgetTracker.Api
dotnet ef database update
```

Verify migration applied:
- Check that `Transactions` table has `embedding` column
- Check that vector indexes were created

### 7. Test RAG Enhancement System

#### Step 1: Import Historical Context Transactions
```http
POST http://localhost:5295/api/transactions/import
X-API-Key: test-key-user1
Content-Type: multipart/form-data

account: Checking Account
file: historical-context.csv (CSV with Starbucks, Shell, Amazon, Tesco, Vodafone)
```

#### Step 2: Manually Categorize Context Transactions
Set categories in database or via API:
- Starbucks → "Food & Drink"
- Shell → "Gas & Fuel"
- Amazon → "Shopping"
- Tesco → "Groceries"
- Vodafone → "Utilities"

#### Step 3: Wait for Embeddings to Generate
- Wait 5+ minutes for background service
- Check API logs for: "Processing embeddings for X recent transactions"

#### Step 4: Import Test Transactions
```http
POST http://localhost:5295/api/transactions/import
X-API-Key: test-key-user1
Content-Type: multipart/form-data

account: Checking Account
file: test-transactions.csv (Similar merchants, different dates/amounts)
```

#### Step 5: Verify Results
Expected behavior:
- ✅ Similar merchants get consistent categories
- ✅ High confidence scores where patterns match
- ✅ Log shows: "Retrieved X context transactions"
- ✅ Enhanced descriptions reflect user patterns

---

## Files Modified/Created

**Created**:
- `src/BudgetTracker.Api/Features/Intelligence/Search/IAzureEmbeddingService.cs`
- `src/BudgetTracker.Api/Features/Intelligence/Search/AzureEmbeddingService.cs`
- `src/BudgetTracker.Api/Features/Intelligence/Search/EmbeddingBackgroundService.cs`
- `src/BudgetTracker.Api/Migrations/[timestamp]_AddVectorEmbeddings.cs`

**Modified**:
- `src/BudgetTracker.Api/Features/Transactions/TransactionTypes.cs` - Added Embedding property
- `src/BudgetTracker.Api/Infrastructure/BudgetTrackerContext.cs` - Configured vector support
- `src/BudgetTracker.Api/Features/Transactions/Import/Enhancement/TransactionEnhancer.cs` - Added RAG logic
- `src/BudgetTracker.Api/Features/Transactions/Import/Enhancement/ITransactionEnhancer.cs` - Updated signature
- `src/BudgetTracker.Api/Features/Transactions/Import/ImportApi.cs` - Pass sessionHash
- `src/BudgetTracker.Api/Program.cs` - Registered new services
- `src/BudgetTracker.Api/Infrastructure/AzureAiConfiguration.cs` - Added EmbeddingDeploymentName

---

## Build Status
✅ Solution builds successfully with no errors or warnings

---

## Key Implementation Details

### RAG Context Retrieval
- Uses cosine distance similarity for semantic matching
- Threshold: < 0.6 distance for relevant transactions
- Limits context to 25 most similar transactions
- Filters by: user, account, date range, category presence
- Excludes current import session to avoid bias

### System Prompt Enhancement
- Base guidelines for merchant identification and categorization
- Dynamic context section with similar transactions
- Examples showing merchant transformation and categories
- Clear scoring guidance (conservative with high scores)

### Embedding Generation
- Combines transaction description + category for richer representation
- Runs asynchronously every 5 minutes
- Processes recent imports (last 24 hours)
- Batch size: 50 transactions
- Graceful error handling with fallback

### Service Architecture
- Uses Microsoft.Extensions.AI abstractions for provider independence
- Dependency injection for testability
- Proper logging throughout
- Cancellation token support for background service
