# Implementation Plan: RAG-Enhanced Transaction Categorization (Step 041)

**Goal**: Implement Retrieval Augmented Generation (RAG) to enhance AI transaction categorization by leveraging historical transaction patterns for improved accuracy and personalization.

**Key Learning Objectives**:
- Vector embeddings and semantic similarity search
- RAG pattern implementation
- Context retrieval and dynamic prompt generation
- Background service for async embedding generation

---

## Part 1: Vector Database Infrastructure Setup

### 1.1 Install pgvector Package
- [ ] From `src/BudgetTracker.Api/`, run: `dotnet add package Pgvector.EntityFrameworkCore --version 0.2.2`
- **Why**: Enables vector operations and semantic search in PostgreSQL
- **Verification**: Check that .csproj includes the package reference

### 1.2 Extend Transaction Entity
- [ ] Update [TransactionTypes.cs](src/BudgetTracker.Api/Features/Transactions/TransactionTypes.cs)
  - Add `using Pgvector;` reference
  - Add property: `public Vector? Embedding { get; set; }` with XML comment explaining 1536 dimensions
- **Why**: Stores semantic embeddings for similarity search
- **Verification**: Property visible in Entity Framework model

### 1.3 Configure Database Context for Vector Support
- [ ] Update [BudgetTrackerContext.cs](src/BudgetTracker.Api/Infrastructure/BudgetTrackerContext.cs)
  - Add `using Pgvector.EntityFrameworkCore;`
  - Add `modelBuilder.HasPostgresExtension("vector");` in `OnModelCreating`
  - Configure Transaction entity with:
    - Vector column type: `vector(1536)`
    - RAG context composite index: `(UserId, Account, Date)` - descending on Date
    - Category index with non-null filter
    - Vector index using HNSW method with cosine operators
- **Why**: Optimizes queries for both RAG context retrieval and semantic search
- **Verification**: All indexes configured correctly

### 1.4 Reset Database and Create New User Account
- [ ] Stop containers: `docker compose down -v`
- [ ] **IMPORTANT**: Verify `docker/docker-compose.yml` has `image: pgvector/pgvector:pg17` (should already be set)
- [ ] Start fresh: `docker compose up -d` (pulls pgvector-enabled PostgreSQL)
- [ ] Start API: `cd src/BudgetTracker.Api/ && dotnet run`
- [ ] Start Web: `cd src/BudgetTracker.Web/ && npm run dev`
- [ ] Register new account at `http://localhost:5173/register` (e.g., `test@example.com` / `P@ssw0rd`)
- [ ] Extract new User ID from network request to `/me` endpoint
- **Why**: Fresh database with pgvector support; user ID needed for config
- **Critical**: Save the new User ID for next step

### 1.5 Update API Configuration with New User ID
- [ ] Update [appsettings.Development.json](src/BudgetTracker.Api/appsettings.Development.json)
  - Replace `<paste-your-new-user-id-here>` with actual User ID from previous step
- [ ] Restart API to apply changes
- **Why**: Links API key to new user account
- **Verification**: API key authentication works with new user

### 1.6 Create Database Migration
- [ ] From `src/BudgetTracker.Api/`, run: `dotnet ef migrations add AddVectorEmbeddings`
- [ ] Apply migration: `dotnet ef database update`
- **Why**: Adds vector column and indexes to production schema
- **Verification**: Migration completes without errors; check pgAdmin that `vector` extension exists

---

## Part 2: Embedding Services Implementation

### 2.1 Deploy Text Embedding Model in Azure AI Foundry
- [ ] Go to https://ai.azure.com/
- [ ] Navigate to **Deployments** → **Deploy model**
- [ ] Search for and select **"text-embedding-3-small"**
- [ ] Set deployment name to **"text-embedding-3-small"**
- [ ] Click "Deploy" and wait for completion (5-10 minutes)
- **Why**: Creates the embedding model endpoint that generates 1536-dimensional vectors
- **Verification**: Deployment shows "Succeeded" status in Azure AI Foundry

### 2.2 Configure Embedding Deployment in User Secrets
- [ ] From `src/BudgetTracker.Api/`, run: `dotnet user-secrets set "AzureAi:EmbeddingDeploymentName" "text-embedding-3-small"`
- [ ] Verify: `dotnet user-secrets list` includes the new setting
- **Why**: Application needs to know which deployment to call for embeddings
- **Verification**: User secrets contain `AzureAi:EmbeddingDeploymentName`

### 2.3 Create Embedding Service Interface
- [ ] Create [IAzureEmbeddingService.cs](src/BudgetTracker.Api/Features/Intelligence/Search/IAzureEmbeddingService.cs)
  - Method: `Task<Vector> GenerateEmbeddingAsync(string text)`
  - Method: `Task<Vector> GenerateTransactionEmbeddingAsync(string description, string? category = null)`
- **Why**: Defines contract for embedding generation; enables dependency injection
- **Verification**: Interface compiles without errors

### 2.4 Implement Azure Embedding Service
- [ ] Create [AzureEmbeddingService.cs](src/BudgetTracker.Api/Features/Intelligence/Search/AzureEmbeddingService.cs)
  - Constructor takes: `IEmbeddingGenerator<string, Embedding<float>>` and `ILogger<AzureEmbeddingService>`
  - `GenerateEmbeddingAsync`: Convert string to vector using IEmbeddingGenerator
  - `GenerateTransactionEmbeddingAsync`: Combine description + category, call GenerateEmbeddingAsync
  - Include error logging and validation
- **Why**: Converts transaction text to semantic vectors; follows Microsoft.Extensions.AI pattern
- **Verification**: Service instantiates correctly; vector generation works

### 2.5 Create Background Service for Automatic Embedding Generation
- [ ] Create [EmbeddingBackgroundService.cs](src/BudgetTracker.Api/Features/Intelligence/Search/EmbeddingBackgroundService.cs)
  - Extends `BackgroundService`
  - Processing interval: 5 minutes
  - Batch size: 50 transactions per cycle
  - Query: Find transactions imported in last 24 hours without embeddings
  - Process: Generate embedding for each transaction, save updates
  - Include: Proper logging, error handling, cancellation support
- **Why**: Asynchronously generates embeddings without blocking API; uses recent transactions only
- **Verification**: Service runs without exceptions; logs appear during execution

---

## Part 3: RAG Integration with Transaction Enhancement

### 3.1 Add RAG Configuration to TransactionEnhancer
- [ ] Update [TransactionEnhancer.cs](src/BudgetTracker.Api/Features/Transactions/Import/Enhancement/TransactionEnhancer.cs)
  - Add constants:
    - `DefaultContextLimit = 25` (number of context transactions)
    - `ContextWindowDays = 365` (time window for retrieval)
  - Update constructor to accept: `IAzureEmbeddingService` and `BudgetTrackerContext`
  - Store as private fields
- **Why**: Configures RAG behavior; makes it easy to experiment with different settings
- **Verification**: Constants and injected dependencies compile

### 3.2 Implement Semantic Context Retrieval
- [ ] Add `GetSemanticContextTransactionsAsync` method to TransactionEnhancer
  - Parameters: `List<string> descriptions`, `string userId`, `string account`, `int limit`, `string excludeImportSessionHash`
  - Generate embedding for combined descriptions
  - Query using raw SQL with cosine distance similarity: `cosine_distance(Embedding, vector) < 0.6`
  - Order by: similarity distance ASC, then Date DESC
  - Filters: UserId, Account, Date >= cutoff, Category != null, ImportSessionHash != current
  - Return: List of similar Transaction objects (up to limit)
  - Include: Proper error handling with fallback to empty list
- **Why**: Retrieves semantically similar historical transactions to inform AI decisions
- **Verification**: Query returns relevant transactions; logs show retrieval counts

### 3.3 Update System Prompt to Include Context
- [ ] Replace `CreateEnhancedSystemPrompt` method in TransactionEnhancer
  - Base prompt: General categorization guidelines (unchanged)
  - Add context section if transactions provided:
    - List 5-10 most similar transactions with amounts and categories
    - Explain that these were selected via semantic similarity
    - Guidance on pattern matching: merchant names, amounts, categories
  - Add examples section (unchanged)
  - Return: Complete enhanced prompt as string
- **Why**: AI uses specific user patterns for better personalization
- **Verification**: Prompt includes retrieved transactions; no errors during prompt building

### 3.4 Update EnhanceDescriptionsAsync Method
- [ ] Modify signature to accept `string currentImportSessionHash` parameter
- [ ] Update method body:
  - Call `GetSemanticContextTransactionsAsync` with descriptions, userId, account, limit, sessionHash
  - Log retrieved context count
  - Generate system prompt with context using `CreateEnhancedSystemPrompt`
  - Create user prompt
  - Call `_chatClient.GetResponseAsync` with system + user messages
  - Parse and return results
  - Include: Proper error handling
- **Why**: Main enhancement flow now uses RAG context
- **Verification**: Method executes without errors; logs show context retrieval

### 3.5 Update ITransactionEnhancer Interface
- [ ] Update [ITransactionEnhancer.cs](src/BudgetTracker.Api/Features/Transactions/Import/Enhancement/ITransactionEnhancer.cs)
  - Add `currentImportSessionHash` parameter to `EnhanceDescriptionsAsync` signature
- **Why**: Interface matches implementation
- **Verification**: Interface compiles

---

## Part 4: Dependency Injection and Testing

### 4.1 Register Services in Program.cs
- [ ] Update [Program.cs](src/BudgetTracker.Api/Program.cs)
  - Register `IEmbeddingGenerator<string, Embedding<float>>` singleton:
    - Create AzureOpenAIClient with credentials
    - Get embedding client and convert to IEmbeddingGenerator
    - Use `config.EmbeddingDeploymentName` from user secrets
  - Register `IAzureEmbeddingService` as scoped
  - Register `ITransactionEnhancer` as scoped (update existing if present)
  - Register `EmbeddingBackgroundService` as hosted service
  - Update `BudgetTrackerContext` DbContext to use `.UseVector()`
- **Why**: Wires all services together; enables dependency injection
- **Verification**: All services register without errors; application starts

### 4.2 Find and Update All Callers of EnhanceDescriptionsAsync
- [ ] Search codebase for calls to `EnhanceDescriptionsAsync`
- [ ] Update each call site to pass `currentImportSessionHash` parameter
- [ ] Likely locations: Transaction import endpoints, batch processing
- **Why**: Method signature changed; all callers must be updated
- **Verification**: Code compiles without errors

### 4.3 Test RAG Enhancement System
- [ ] **Step 1**: Import historical context transactions
  - Use provided CSV in task (Starbucks, Shell, Amazon, Tesco, Vodafone)
  - Account: "Checking Account"
- [ ] **Step 2**: Manually categorize context transactions via database
  - Starbucks → "Food & Drink"
  - Shell → "Gas & Fuel"
  - Amazon → "Shopping"
  - Tesco → "Groceries"
  - Vodafone → "Utilities"
- [ ] **Step 3**: Wait 5+ minutes for background service to generate embeddings
  - Check logs for "Processing embeddings for X recent transactions"
- [ ] **Step 4**: Import test transactions (similar to context set)
  - New Starbucks, Shell, Amazon, Tesco, Vodafone transactions
  - Different store numbers/dates but same merchants
- [ ] **Step 5**: Verify results
  - Categories should match historical patterns
  - Confidence scores should be high for similar merchants
  - Description enhancements should be consistent with historical data
- **Why**: Validates RAG system works end-to-end
- **Expected Results**:
  - Context transactions retrieved (log: "Retrieved X context transactions")
  - Enhanced descriptions reflect user patterns
  - Consistent categorization for similar merchants
  - High confidence scores where patterns match

---

## Verification Checklist

- [ ] pgvector package installed
- [ ] Transaction entity has Embedding property
- [ ] Database configured with vector extension and indexes
- [ ] Database migration applied successfully
- [ ] Text-embedding-3-small deployed in Azure AI Foundry
- [ ] EmbeddingDeploymentName configured in user secrets
- [ ] IAzureEmbeddingService interface created
- [ ] AzureEmbeddingService implementation works
- [ ] EmbeddingBackgroundService runs without errors
- [ ] TransactionEnhancer uses RAG configuration and context retrieval
- [ ] System prompt includes retrieved context
- [ ] EnhanceDescriptionsAsync uses RAG flow
- [ ] ITransactionEnhancer interface updated
- [ ] All DI registrations in Program.cs
- [ ] All callers of EnhanceDescriptionsAsync updated
- [ ] Background service generates embeddings automatically
- [ ] RAG retrieval returns semantically similar transactions
- [ ] Enhanced categorization reflects user patterns

---

## Notes

**Time Estimate**: 2-3 hours (includes Azure deployment wait time)

**Key Files to Create/Modify**:
- Create: `Features/Intelligence/Search/IAzureEmbeddingService.cs`
- Create: `Features/Intelligence/Search/AzureEmbeddingService.cs`
- Create: `Features/Intelligence/Search/EmbeddingBackgroundService.cs`
- Modify: `Features/Transactions/TransactionTypes.cs`
- Modify: `Infrastructure/BudgetTrackerContext.cs`
- Modify: `Features/Transactions/Import/Enhancement/TransactionEnhancer.cs`
- Modify: `Features/Transactions/Import/Enhancement/ITransactionEnhancer.cs`
- Modify: `Program.cs`
- Modify: `appsettings.Development.json`

**Critical Decisions**:
- Vector dimension: 1536 (for text-embedding-3-small model)
- Cosine distance threshold: 0.6 (for semantic similarity)
- Context window: 365 days (1 year of history)
- Batch size: 50 transactions per processing cycle
- Processing interval: 5 minutes between checks

**Potential Issues & Solutions**:
1. **Database reset loses data**: Expected behavior; only occurs once when enabling pgvector
2. **Embeddings not generating**: Check EmbeddingBackgroundService logs and Azure deployment status
3. **RAG context empty**: Historical transactions need categories before use; populate manually
4. **Slow embedding generation**: Consider reducing batch size or increasing interval
