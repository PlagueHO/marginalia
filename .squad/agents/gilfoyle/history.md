# Project Context

- **Owner:** Mary MacGregor-Reid
- **Project:** Marginalia — AI-powered narrative flow editor for long-form non-fiction writers. Helps authors refine, expand, and ensure stylistic consistency in manuscripts with AI-generated suggestions and fine-grained editorial control.
- **Stack:** React (frontend), .NET 10 / ASP.NET (backend), Azure Functions, Azure AI / Microsoft Foundry, Google Docs API integration
- **Created:** 2026-03-22

## Learnings

### Analyze Endpoint URL & Response Type Fix (2026-03-22T09:27:00Z)

**Agent:** Dinesh (Frontend Dev)  
**Status:** ✅ COMPLETE — Frontend now correctly calls `/api/documents/{id}/analyze` with `Suggestion[]` response.

**Summary:** Frontend was calling `/api/documents/analyze` (missing document ID) and expected `AnalyzeResponse` wrapper instead of direct `Suggestion[]`. Fixed URL to include `{id}` path parameter, updated response type contract, and removed obsolete wrapper type.

**Files Updated:**

- `src/services/documentService.ts` — URL and response type corrected
- `src/hooks/useAnalysis.ts` — adapted to `Suggestion[]` response
- `src/types/api.ts`, `src/types/index.ts` — removed `AnalyzeResponse` export

**Impact:** Frontend analyze flow now correctly integrates with backend `/api/documents/{id}/analyze` endpoint. Build clean, types aligned.

---

### Config Endpoint — IChatClient Metadata Fix (2026-03-22T08:39:00Z)

**Status:** ✅ COMPLETE — Build 0 warnings/errors, 78 unit tests pass.

**Summary:** `GET /api/config/llm` was returning placeholder values from `appsettings.Development.json` instead of the real endpoint and model the Aspire-managed `IChatClient` is connected to. Fixed by reading `ChatClientMetadata` from the injected `IChatClient` at request time.

**Root Cause:** Two disconnected config paths — `LlmEndpointOptions` (bound to appsettings) vs Aspire's `ai-foundry` connection string. The controller read from path #1; the actual client used path #2.

**Fix (Approach A — IChatClient metadata):**

- `ConfigController.GetLlmConfig()` now calls `_chatClient?.GetService<ChatClientMetadata>()` to get `ProviderUri` and `DefaultModelId`
- Falls back to `LlmEndpointOptions` when no metadata available (non-Aspire scenarios)
- No changes to AppHost, no hardcoded values

**Cleanup:**

- `appsettings.Development.json` — removed placeholder endpoint/model and stale `ApiKey` field; `LlmEndpoint` section now has empty values (Aspire provides real ones)
- `appsettings.json` — removed stale `ApiKey` field from `LlmEndpoint` section

**Key API:**

- `IChatClient.GetService<ChatClientMetadata>()` returns `ChatClientMetadata` with `ProviderUri` (Uri), `DefaultModelId` (string), `ProviderName` (string)
- Aspire's `AddAzureChatCompletionsClient().AddChatClient()` populates this metadata automatically

**Key Files:**

- `src/Api/Controllers/ConfigController.cs` — metadata-aware `GetLlmConfig()`
- `src/Api/appsettings.Development.json` — cleaned up
- `src/Api/appsettings.json` — cleaned up

**Orchestration Log:** `.squad/orchestration-log/2026-03-22T08_39_00Z-gilfoyle.md`
**Session Log:** `.squad/log/2026-03-22T08_39_00Z-config-endpoint-fix.md`

### CORS Dynamic Origin Fix (2026-03-22)

**Status:** ✅ COMPLETE — Compiles clean (no C# errors).

**Summary:** Replaced hardcoded `WithOrigins("http://localhost:5173")` with `SetIsOriginAllowed` using an `IsLocalOrigin` predicate. Aspire assigns a random Vite port at startup so the old hardcoded value broke all preflight requests.

**Key File:** `marginalia-service/src/Api/Program.cs` (lines ~54–80)

**Pattern:**

- Read `CORS:AllowedOrigins` from config (supports `CORS__AllowedOrigins` env var double-underscore convention from Bicep).
- If empty (dev): `SetIsOriginAllowed(IsLocalOrigin)` — any localhost/127.0.0.1 port allowed.
- If set (prod): explicit list + localhost always permitted.
- `static bool IsLocalOrigin(string origin)` uses `Uri.TryCreate` + pattern match on `uri.Host`.

**Decision logged:** `.squad/decisions/inbox/gilfoyle-cors-dynamic-origins.md` → merged to decisions.md 2026-03-22T08:07:46Z

**Orchestration Log:** `.squad/orchestration-log/2026-03-22T08_07_46Z-gilfoyle.md`

### Entra-Only Auth — Aspire Azure AI Inference Migration (2026-03-22)

**Status:** ✅ COMPLETE — Build 0 warnings/errors, 78 unit tests pass.

**Summary:** Removed all API key authentication paths. Backend now authenticates to Azure AI Foundry exclusively via DefaultAzureCredential (Entra ID) through the Aspire Azure AI Inference client integration.

**Package Changes:**

- Removed: `Aspire.Azure.AI.OpenAI`, `Azure.AI.OpenAI`, `OpenAI`
- Added: `Aspire.Azure.AI.Inference` 13.1.3-preview.1.26166.8
- Kept: `Azure.Identity`, `Microsoft.Extensions.AI.Abstractions`

**Key File Changes:**

- `Directory.Packages.props` — replaced OpenAI Aspire/SDK packages with Aspire.Azure.AI.Inference
- `LlmEndpointOptions.cs` — removed `ApiKey` property; Entra ID only
- `Program.cs` — removed `FOUNDRY_API_KEY` env var block; replaced `AddAzureOpenAIClient` + manual IChatClient singleton with `AddAzureChatCompletionsClient("ai-foundry").AddChatClient("chat")`
- `FoundrySuggestionService.cs` — removed dual-path (Aspire vs SDK fallback), all OpenAI SDK code, Azure.Identity imports; IChatClient is now required (ArgumentNullException if null); constructor takes only `IChatClient` + `ILogger`
- `ConfigController.cs` — removed `UpdateLlmConfig` POST/PUT, removed `IConfiguration` dep, removed `MaskApiKey`; injects `IChatClient?` (optional); `GetLlmConfig` returns `authMethod: "entraId"` always; new `GET /api/config/llm/health` endpoint
- `LlmConfigResponse.cs` — removed `ApiKey` property
- `LlmConfigRequest.cs` — DELETED (no more POST endpoint)
- `LlmHealthResponse.cs` — NEW: `{ healthy: bool, message: string }`
- `LlmEndpointOptionsTests.cs` — removed ApiKey tests (Constructor_WithAllFields, Record_With, ApiKey_CanBeEmptyString)

**Patterns:**

- `AddAzureChatCompletionsClient(name).AddChatClient(deployment)` is the Aspire AI Inference one-liner that auto-registers `IChatClient` with DefaultAzureCredential
- `IChatClient?` (nullable) in ConfigController enables health check to return false without crashing when Aspire not running
- `IChatClient` (required, non-nullable) in FoundrySuggestionService means analysis endpoints fail gracefully with DI error when not under Aspire
- Health endpoint `GET /api/config/llm/health` is lightweight (no token spend) — checks presence of IChatClient registration only

### Backend Implementation (2026-03-22)

**Architecture:**

- Clean Architecture: Domain (models/interfaces) → Infrastructure (implementations) → Api (controllers/DI)
- In-memory storage via `ConcurrentDictionary` — no database per spec
- `IOptionsMonitor<LlmEndpointOptions>` for hot-reloadable LLM config
- `HttpClient` via `AddHttpClient<ISuggestionService, FoundrySuggestionService>()` for Foundry calls

**Key Files:**

- `src/Api/Program.cs` — DI, CORS (localhost:5173), JSON config, Kestrel 50MB limit
- `src/Api/Controllers/DocumentsController.cs` — upload, paste, analyze, export, suggestion CRUD
- `src/Api/Controllers/SessionsController.cs` — session create/get
- `src/Api/Controllers/ConfigController.cs` — BYO model config (masked API key)
- `src/Infrastructure/Repositories/InMemoryDocumentRepository.cs` — thread-safe document store
- `src/Infrastructure/Repositories/InMemorySessionRepository.cs` — thread-safe session store
- `src/Infrastructure/Services/FoundrySuggestionService.cs` — Foundry API client, chunking, prompt building
- `src/Infrastructure/Services/WordDocumentService.cs` — .docx parse/export via OpenXml
- `src/Domain/Interfaces/IWordDocumentService.cs` — Word import/export contract
- `src/Domain/Interfaces/ISessionRepository.cs` — session storage contract
- `src/Domain/Models/AnalysisRequest.cs` — analysis trigger DTO

**Patterns:**

- Type alias `DomainDocument = Marginalia.Domain.Models.Document` in WordDocumentService to resolve OpenXml ambiguity
- Source-generated JSON serializer context (`FoundrySerializerContext`) for Foundry API DTOs
- Document chunking at ~6000 chars (~3 pages), breaks on paragraph boundaries
- Accepted suggestions applied in reverse order (descending start offset) during export
- Env vars `FOUNDRY_ENDPOINT`, `FOUNDRY_API_KEY`, `FOUNDRY_MODEL_NAME` override appsettings

### Aspire Integration (2026-03-22)

**Architecture:**

- Aspire 13.1.3 AppHost SDK orchestrates API + Vite frontend + Azure AI Foundry
- ServiceDefaults project provides OpenTelemetry, health checks, resilience, service discovery
- `IChatClient` (Microsoft.Extensions.AI) injected via Aspire when connection string `ai-foundry` present
- `FoundrySuggestionService` dual-path: `IChatClient` (Aspire) or raw `HttpClient` (standalone/BYO)

**Key Files:**

- `src/Orchestration/AppHost/AppHost.cs` — AI Foundry + chat deployment + Vite frontend orchestration
- `src/Orchestration/AppHost/Properties/launchSettings.json` — ports 17280/15240, dashboard endpoints
- `src/Orchestration/ServiceDefaults/Extensions.cs` — AddServiceDefaults/MapDefaultEndpoints pattern
- `src/Api/Program.cs` — `builder.AddServiceDefaults()`, conditional `AddAzureOpenAIClient`, `app.MapDefaultEndpoints()`

**Package Versions (Aspire):**

- Aspire.AppHost.Sdk: 13.1.3
- Aspire.Hosting.Azure.AIFoundry: 13.1.3-preview.1.26166.8
- Aspire.Azure.AI.OpenAI: 13.1.3-preview.1.26166.8
- Aspire.Hosting.JavaScript: 13.1.3
- Aspire.Hosting.Testing: 9.2.0
- OpenTelemetry.*: 1.15.x
- Microsoft.Extensions.Http.Resilience/ServiceDiscovery: 10.3.0
- Microsoft.Extensions.AI.Abstractions: 9.5.0

**Patterns:**

- Internal DTOs renamed to `FoundryApi*` to avoid namespace conflict with `Microsoft.Extensions.AI.ChatMessage`
- Conditional IChatClient registration: only when `ConnectionStrings:ai-foundry` is present
- `AsIChatClient()` extension bridges OpenAI SDK `ChatClient` → `IChatClient`
- Nested integration test project excluded from parent via `<Compile Remove="Orchestration.IntegrationTests\**" />`

**Session Completion (2026-03-22 Session Log):**

- Full Aspire orchestration layer implemented with zero warnings
- 79 backend tests passing
- AppHost is solution `defaultStartup` project
- Orchestration decision documented and merged into team decisions.md
- Integration log created: `.squad/orchestration-log/2026-03-22T01_25_00Z-gilfoyle.md`

### Infrastructure Bicep Setup (2026-03-22)

**Status:** ✅ COMPLETE — `az bicep build` passes with zero errors.

**Source:** Infrastructure ported from <https://github.com/PlagueHO/prompt-babbler> (public repo, depth-1 clone, then cleaned up).

**Key Files:**

- `infra/main.bicep` — Adapted from prompt-babbler; all "promptbabbler"/"prompt-babbler" refs → "marginalia". Cosmos DB schema updated to Marginalia data model (documents/sessions/suggestions). Container image → `ghcr.io/marymacgregorreid/marginalia-service:latest`. RAI policy renamed to `MarginaliaContentPolicy`.
- `infra/main.bicepparam` — Unchanged structure, reads from env vars.
- `infra/cognitive-services/accounts/` — Copied byte-for-byte from source (8 .bicep files across 5 subdirs).
- `infra/core/security/role_foundry.bicep` — Foundry role assignment helper.
- `infra/entra-id/app-registrations.bicep` — Entra ID app registration automation.
- `infra/hooks/preprovision.ps1`, `preprovision.sh` — Pre-provision scripts for Entra ID setup.
- `infra/abbreviations.json`, `bicepconfig.json`, `model-deployments.json` — Supporting config.

**Architecture:**

- `targetScope = 'subscription'` — deploys resource group first, all resources scoped to it.
- VNet with two subnets (ACA at 10.0.0.0/23, private endpoints at 10.0.2.0/24).
- Private DNS zones for Cosmos DB, Foundry (cognitiveservices), and OpenAI endpoints.
- Azure AI Foundry (AIServices kind, S0 SKU) with managed identity + project named "marginalia".
- Cosmos DB serverless with private endpoint, three containers: documents/sessions/suggestions.
- Container Apps Environment (VNet-integrated) hosting marginalia-service .NET backend.
- Static Web App (Free tier) for marginalia-app React frontend.
- Role assignments: Cognitive Services OpenAI User for Container App MI; Data Contributor for Cosmos DB.

**Bicep CLI Note:**

- Standalone `bicep` binary v0.34.44 fails on `@secure()` output decorators (BCP129).
- `az bicep` v0.41.2 compiles cleanly. Use `az bicep build` for local validation.
- `@secure()` on outputs is stable in Bicep 0.35+; `az bicep` bundles the newer version.

**Omissions (per spec):**

- No `main.json` files (compiled output excluded from all subdirectories).
- No `keyVaultExport.json`.

### Team Update — Foundry Proxy Fix Session (2026-03-22T03:35:00Z)

**From Dinesh's work:** Frontend `configService` updated from PUT to POST for config save, with proper error feedback via toasts. All frontend HTTP already routes through backend proxy to `localhost:5279`. Build clean, 88 tests pass.

**Coordination Note:** Backend's Foundry proxy now correctly uses `/openai/deployments/{model}/chat/completions` URL format and `api-key` header (not Bearer). Frontend config POST and backend Foundry HTTP are now aligned.

### Entra-Only Auth — Exclusive Aspire Azure AI Inference Integration (2026-03-22T07:20:00Z)

**Status:** ✅ COMPLETE — Build 0 warnings/errors, 78 unit tests pass.

**Summary:** Removed all API key authentication paths. Backend now authenticates to Azure AI Foundry exclusively via DefaultAzureCredential (Entra ID) through the Aspire Azure AI Inference client integration. No dual-path fallbacks; Aspire is required at runtime.

**Breaking Changes:**

- Standalone mode (API key + HttpClient) removed entirely.
- `/api/config/llm` POST/PUT endpoints removed.
- Frontend can no longer POST credentials.
- `FoundrySuggestionService.AnalyzeAsync` requires registered `IChatClient` — fails with DI exception if not under Aspire.

**Package Changes:**

- Removed: `Aspire.Azure.AI.OpenAI`, `Azure.AI.OpenAI`, `OpenAI`, `Azure.Identity`
- Added: `Aspire.Azure.AI.Inference` 13.1.3-preview.1.26166.8
- Kept: `Azure.Identity`, `Microsoft.Extensions.AI.Abstractions`

**Key File Changes:**

- `Directory.Packages.props` — removed OpenAI Aspire/SDK packages; added Aspire.Azure.AI.Inference
- `LlmEndpointOptions.cs` — removed `ApiKey` property
- `Program.cs` — one-liner: `builder.AddAzureChatCompletionsClient("ai-foundry").AddChatClient("chat")`
- `FoundrySuggestionService.cs` — constructor requires `IChatClient` (non-nullable); removed all OpenAI SDK code and fallback paths
- `ConfigController.cs` — removed POST/PUT endpoints; `GetLlmConfig` injects `IChatClient?`; new `GET /api/config/llm/health` endpoint
- `LlmConfigResponse.cs` — removed `ApiKey` property
- `LlmHealthResponse.cs` — NEW: `{ healthy: bool, message: string }`
- `LlmConfigRequest.cs` — DELETED
- Tests — removed ApiKey-related tests from `LlmEndpointOptionsTests.cs`

**Patterns:**

- `AddAzureChatCompletionsClient(name).AddChatClient(deployment)` auto-registers `IChatClient` with `DefaultAzureCredential`
- `IChatClient?` (nullable) in ConfigController enables health check to return false without crashing when Aspire not running
- `IChatClient` (required, non-nullable) in FoundrySuggestionService ensures analysis fails gracefully with DI error when not under Aspire
- Health endpoint `GET /api/config/llm/health` is lightweight (no token spend) — checks presence of IChatClient registration only

**Orchestration Log:** `.squad/orchestration-log/2026-03-22T07_20_00Z-gilfoyle.md`
**Decision:** `.squad/decisions/decisions.md` → Entra-Only Authentication section

### Backend Structured Logging (2026-03-22)

**Status:** ✅ COMPLETE — Build 0 warnings/errors, 78 unit tests pass.

**Summary:** Added ILogger injection and structured logging to all three API controllers (ConfigController, SessionsController, DocumentsController) and startup diagnostic logging to Program.cs. Follows the prompt-babbler reference pattern.

**Key Changes:**

- `ConfigController.cs` — `ILogger<ConfigController>` injected; logs LLM config requests (LogInformation) and health check results (LogInformation for healthy, LogWarning for unhealthy)
- `SessionsController.cs` — `ILogger<SessionsController>` injected; logs session creation and retrieval (LogInformation), not-found as LogWarning
- `DocumentsController.cs` — `ILogger<DocumentsController>` injected; logs document upload/paste/analysis/export (LogInformation), not-found and invalid file type as LogWarning
- `Program.cs` — Startup diagnostic logger emits presence/absence of AI Foundry connection string, FOUNDRY_ENDPOINT, CORS mode, and OTEL exporter endpoint

**Patterns:**

- Structured logging with message templates (no string interpolation) — e.g., `_logger.LogInformation("Document uploaded: {DocumentId}, FileName: {FileName}", ...)`
- Startup logger uses `LoggerFactory.Create(b => b.AddConsole())` for pre-Build() diagnostics (same as prompt-babbler reference)
- Never logs sensitive values (connection strings, keys) — only presence/absence
- LogWarning for not-found and validation failures; LogInformation for successful operations

### Backend Structured Logging (2026-03-22T08:15:00Z)

**Status:** ✅ COMPLETE — Build 0 warnings/errors, 78 unit tests pass.

**Outcome Verification:**

- Orchestration log: `.squad/orchestration-log/2026-03-22T08_15_00Z-gilfoyle.md`
- Decision merged: `.squad/decisions.md` → Backend Structured Logging Pattern section
- All 4 files compile and pass tests
- Startup diagnostics operational (logs connection string, endpoint, CORS mode, OTEL status)
- All 3 controllers emit structured logs with zero sensitive data exposure

**Integration with Telemetry Stack:**

- Backend logs (this work) + Frontend OTel SDK (Dinesh) provide end-to-end tracing
- Session log: `.squad/log/2026-03-22T08_15_00Z-telemetry-improvement.md`
- Next: Monitor Aspire dashboard trace correlation quality

### Config Endpoint — IChatClient Metadata Fix (2026-03-22)

**Status:** ✅ COMPLETE — Build 0 warnings/errors, 78 unit tests pass.

**Summary:** `GET /api/config/llm` was returning placeholder values from `appsettings.Development.json` instead of the real endpoint/model the Aspire-managed `IChatClient` is connected to. Fixed by reading `ChatClientMetadata` from the injected `IChatClient` at request time.

**Root Cause:** Two disconnected config paths — `LlmEndpointOptions` (bound to appsettings) vs Aspire's `ai-foundry` connection string. The controller read from path #1; the actual client used path #2.

**Fix (Approach A — IChatClient metadata):**

- `ConfigController.GetLlmConfig()` now calls `_chatClient?.GetService<ChatClientMetadata>()` to get `ProviderUri` and `DefaultModelId`
- Falls back to `LlmEndpointOptions` when no metadata available (non-Aspire scenarios)
- No changes to AppHost, no hardcoded values

**Cleanup:**

- `appsettings.Development.json` — removed placeholder endpoint/model and stale `ApiKey` field; `LlmEndpoint` section now has empty values (Aspire provides real ones)
- `appsettings.json` — removed stale `ApiKey` field from `LlmEndpoint` section

**Key API:**

- `IChatClient.GetService<ChatClientMetadata>()` returns `ChatClientMetadata` with `ProviderUri` (Uri), `DefaultModelId` (string), `ProviderName` (string)
- Aspire's `AddAzureChatCompletionsClient().AddChatClient()` populates this metadata automatically

**Key Files:**

- `src/Api/Controllers/ConfigController.cs` — metadata-aware `GetLlmConfig()`
- `src/Api/appsettings.Development.json` — cleaned up
- `src/Api/appsettings.json` — cleaned up

### Upload/Paste Response Type Fix (2026-03-22)

**Status:** ✅ COMPLETE — 78 unit tests pass, Domain builds clean.

**Summary:** Both `/api/documents/upload` and `/api/documents/paste` were returning raw `Document` objects. Frontend expects `{ document: Document, sessionId: string }`. Added `UploadDocumentResponse` DTO and injected `ISessionRepository` into `DocumentsController`. Each endpoint now creates a `UserSession` and returns the wrapped response.

**Root Cause:** `CreatedAtAction(..., document)` returned the document directly. `response.document` was `undefined` in the frontend → TypeError silently swallowed → "Failed to process text" toast.

**Key Changes:**

- `src/Domain/Models/UploadDocumentResponse.cs` — NEW sealed record with `[JsonPropertyName]` attributes
- `src/Api/Controllers/DocumentsController.cs` — `ISessionRepository` injected; `Upload` and `Paste` return `ActionResult<UploadDocumentResponse>`; `[RequestSizeLimit]` added to `Paste`

**Patterns:**

- `UploadDocumentResponse` lives in Domain/Models alongside other response DTOs
- Session created at document ingestion time (not deferred); one session per upload/paste
- `ISessionRepository` was already registered as singleton — zero Program.cs changes needed

**Decision:** `.squad/decisions/inbox/gilfoyle-paste-upload-response-fix.md`

### Deployment Rename: foundry to reviewer (2026-03-22)

**Status:** Complete — Build 0 warnings/errors, 78 unit tests pass.

**Summary:** Renamed the AI model deployment from `foundry` to `reviewer` across the codebase. The old name was ambiguous. The new name reflects the deployment's purpose: reviewing/analyzing manuscripts.

**Changes:**

- AppHost.cs — deployment resource name foundry -> reviewer; variable modelDeployment -> reviewerDeployment
- Program.cs — AddChatClient(foundry) -> AddChatClient(reviewer)

**Not changed:** ai-foundry references (account/resource name, not deployment name) remain untouched.

### Cosmos DB Persistence with Preview Emulator (2026-03-29T00:10:33Z)

**Status:** ✅ COMPLETE — Build succeeded, 78 unit tests pass.

**Summary:** Replaced in-memory ConcurrentDictionary storage with Cosmos DB persistence using the Azure Cosmos DB preview emulator. Followed the PlagueHO/prompt-babbler reference pattern exactly.

**Package Changes:**

- AppHost: Added Aspire.Hosting.Azure.CosmosDB 13.2.0
- Api: Added Aspire.Microsoft.Azure.Cosmos 13.2.0
- Infrastructure: Added Microsoft.Azure.Cosmos 3.58.0, Newtonsoft.Json 13.0.4

**Key File Changes:**

**AppHost.cs:**

- Added Cosmos preview emulator with Data Explorer
- Created database "marginalia" with 3 containers: documents, sessions, suggestions
- Partition key /userId for all containers
- Wired containers to API service

**Domain Models:**

- Document.cs, UserSession.cs, Suggestion.cs — Added UserId property with default "_anonymous"

**Repository Interfaces:**

- IDocumentRepository — Added userId parameter; replaced GetBySessionAsync with GetByUserAsync; added DeleteAsync
- ISessionRepository — Added userId parameter to all methods

**Repository Implementations:**

- CosmosDocumentRepository.cs — NEW: Uses CosmosClient.GetContainer(), PartitionKey(userId)
- CosmosSessionRepository.cs — NEW: Same pattern
- InMemory repositories updated to match new interface

**API:**

- Program.cs — Added CosmosClient registration with System.Text.Json serializer
- DocumentsController.cs — Added GetUserId(Request) helper; extracts X-User-Id header
- SessionsController.cs — Same userId extraction pattern

**Tests:**

- DocumentRepositoryContractTests.cs — Updated to pass userId parameter

**Patterns:**

- Partition key /userId enables multi-tenant data isolation
- X-User-Id header extraction with "_anonymous" default
- Structured logging for all Cosmos CRUD operations
- InMemory repositories retained for test compatibility

### Home Page Backend API (2026-03-29)

**Status:** ✅ COMPLETE — Build clean (0 warnings/errors), 113 unit tests pass.

**Summary:** Implemented backend changes for home page document listing feature per Richard's API design.

**Model Changes:**

- `Document.cs` — added `Title`, `Status` (DocumentStatus), `CreatedAt`, `UpdatedAt` with record defaults for backward-compatible Cosmos deserialization
- New: `DocumentStatus.cs` (Draft/Analyzed), `DocumentSummary.cs` (listing DTO), `DocumentListResponse.cs` (wrapper)
- `PasteDocumentRequest.cs` — added optional `Title`

**API Changes:**

- `GET /api/documents` — `DocumentListResponse` with projections sorted by UpdatedAt desc; handles legacy docs (missing title → filename, missing status → inferred from suggestion count)
- `POST /api/documents/upload` — accepts `[FromForm] string? title`, defaults to `"{now} - {filename}"`, sets Draft + timestamps
- `POST /api/documents/paste` — accepts `title` in body, defaults to `"{now} - {filename|Untitled}"`, sets Draft + timestamps
- `POST /api/documents/{id}/analyze` — sets `Status=Analyzed`, `UpdatedAt=UtcNow` after analysis

**Key Files:**

- `src/Domain/Models/Document.cs`, `DocumentStatus.cs`, `DocumentSummary.cs`, `DocumentListResponse.cs`, `PasteDocumentRequest.cs`
- `src/Api/Controllers/DocumentsController.cs`

**Design Notes:**

- No repository or interface changes needed — `GetByUserAsync` already existed
- Legacy Cosmos documents deserialize cleanly — new fields use record defaults (empty title, Draft status, MinValue timestamps)
- List endpoint projection handles fallbacks at query time (empty title → filename, status inferred from suggestion count)

### 2026-03-29 — Cross-Agent: Home Page Feature Complete

**Richard (Lead):** API design accepted and implemented as specified. Flat REST hierarchy decision upheld.

**Dinesh (Frontend):** React Router v7 with HomePage consuming `GET /api/documents`. Title input on DocumentUploader sends optional title on upload/paste. Build clean, 82+ tests pass.

**Jared (Tester):** 42 backend tests covering all new DTOs, enum serialization, status transitions, title defaults. All pass against Gilfoyle's implementation.
