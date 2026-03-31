# Squad Decisions

## Active Decisions

### Infrastructure (2026-03-22)

1. **Marginalia Infrastructure Bicep:** Port Bicep IaC from PlagueHO/prompt-babbler (reference impl using identical stack) into `infra/` with Marginalia-specific adaptations:
    - **Project identity:** All "prompt-babbler" references → "marginalia"
    - **Foundry project:** `defaultProjectName = 'marginalia'`
    - **Cosmos DB schema:** Containers for `documents` (by `/sessionId`), `sessions` (by `/id`), `suggestions` (by `/documentId`)
    - **Container image:** `ghcr.io/marymacgregorreid/marginalia-service:latest`
    - **RAI policy:** `MarginaliaContentPolicy`
    - **Speech Services:** Removed (not used in Marginalia)
    - **Validation:** `az bicep build` compiles clean (v0.41.2). Note: standalone `bicep` CLI v0.34.44 has BCP129 limitation; use `az bicep`.
    - **azd-compatible:** Tags and Container App/Static Web App naming follow Azure Developer CLI conventions.

### Implementation Sprint Decisions (2026-03-22)

1. **In-Memory Data Storage:** Marginalia uses ConcurrentDictionary for document and session storage per specification. No database layer at this time. Backend validated with contract tests; ready for migration to persistent storage.

1. **No Global State Management Frontend:** React hooks (`useDocument`, `useSuggestions`, `useAnalysis`, `useLlmConfig`) manage state at component tree level. No Redux/Zustand/Context API required at current scale.

1. **Suggestion Accessibility Pattern:** Suggestions use color + icon combination (not color-only) for WCAG compliance. Icons: AlertCircle (pending), Check (accepted), X (rejected), Pencil (modified).

1. **Nested-Interactive Components Flagged:** SuggestionCard and DocumentUploader have nested interactive elements (axe violation). Resolution pending — move buttons outside focusable headers or use aria-owns.

1. **BYO Model Configuration:** Backend supports hot-reloadable LLM endpoint configuration via `IOptionsMonitor<LlmEndpointOptions>`. API key masked in display. Env vars override appsettings.

1. **Document Chunking Strategy:** Splits at ~6000 characters on paragraph boundaries to respect context windows. Suggestions applied in reverse order during export to preserve offsets.

1. **Quickstart Documentation Pattern:** Created two quickstart guides following the PlagueHO/prompt-babbler project pattern. QUICKSTART-LOCAL.md is complete and functional for .NET Aspire local development. QUICKSTART-AZURE.md is a placeholder documenting the planned architecture (Container Apps, Static Web Apps, AI Foundry) with explicit disclaimer for when infrastructure templates are built. Enables immediate contributor onboarding while preserving infrastructure flexibility.

### Authentication & Authorization (2026-03-22)

1. **Foundry API Proxy — OpenAI SDK:** Use the `OpenAI` NuGet SDK (2.7.0) for the standalone/BYO Foundry path instead of raw HttpClient. The SDK handles both v1 and classic endpoint patterns via `OpenAIClientOptions.Endpoint`.
   - **Flow:** Frontend pushes config → `POST /api/config/llm` → Backend stores in `IOptionsMonitor<LlmEndpointOptions>` → Analysis triggers → Backend creates `OpenAIClient` with stored config → `ChatClient.CompleteChatAsync()`.
   - **Consequences:** No manual URL construction. SDK handles v1 vs classic transparently. Frontend never contacts Foundry directly. API key stays server-side after config push. HttpClient dependency removed from FoundrySuggestionService.

1. **Entra ID Authentication Fallback:** Add `DefaultAzureCredential` as a fallback authentication method when no API key is provided. The system supports three authentication paths:
   - **API Key** (`authMethod: "apiKey"`) — `OpenAIClient` with `ApiKeyCredential`. Existing behavior, unchanged.
   - **Entra ID** (`authMethod: "entraId"`) — `AzureOpenAIClient` with `DefaultAzureCredential`. Used when endpoint is configured but no API key is present.
   - **None** (`authMethod: "none"`) — No endpoint configured at all.
   - **Consequences:** Users with Azure CLI credentials can use Foundry without an API key. Frontend can display which authentication method is in use. New packages: `Azure.AI.OpenAI` 2.1.0, `Azure.Identity` 1.19.0. Frontend shows "API Key (Optional)" with auth method indicator badge (API Key vs Entra ID).

### Backend Structured Logging Pattern (2026-03-22)

1. **Structured Logging with ILogger:** All API controllers inject `ILogger<T>` via constructor; startup diagnostics use a standalone `LoggerFactory.Create(b => b.AddConsole())` logger. Message templates use `{PropertyName}` placeholders (no string interpolation). Never log secrets — connection strings and keys logged as `(set)` / `(not set)` only.
   - **Controllers:** ConfigController, SessionsController, DocumentsController all emit structured logs for successful operations (LogInformation) and errors (LogWarning).
   - **Startup:** Program.cs emits diagnostic logs on presence/absence of AI Foundry connection string, FOUNDRY_ENDPOINT env var, CORS mode, and OTEL exporter endpoint.
   - **Pattern:** Matches prompt-babbler reference implementation. Structured logs integrate with OpenTelemetry already configured in ServiceDefaults, enabling trace correlation in Aspire dashboard without additional packages.

### OpenTelemetry Browser SDK (2026-03-22)

1. **Browser Telemetry for Aspire Dashboard:** Frontend added 13 `@opentelemetry/*` packages with WebTracerProvider + MeterProvider. Auto-instrumentations enabled for document load, fetch, and user interaction. OTLP exports to `OTEL_EXPORTER_OTLP_ENDPOINT` when set; gracefully no-ops when absent (standalone `pnpm dev`).
   - **Integration:** Vite `define` forwards 4 OTEL env vars (`OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_HEADERS`, `OTEL_RESOURCE_ATTRIBUTES`, `OTEL_SERVICE_NAME`) from environment at build time.
   - **Initialization:** `src/telemetry.ts` checks endpoint presence before SDK setup; `src/main.tsx` calls `initTelemetry()` before React renders.
   - **Exports:** `tracer`, `meter`, and `endSpanWithDuration` helper available for custom instrumentation.
   - **Consequence:** Bundle size increases ~200KB gzipped (OTel SDK + zone.js). End-to-end tracing: browser → backend → Foundry now visible in Aspire dashboard. No test changes needed — telemetry no-ops in test environment.
   - **Pattern:** Matches prompt-babbler reference (identical stack, identical OTel setup).

### Service Discovery & CORS (2026-03-22)

1. **Dynamic CORS Origin Matching:** Replace hardcoded `WithOrigins("http://localhost:5173")` with `SetIsOriginAllowed` using a local-origin predicate.
   - **Rationale:** Aspire AppHost assigns a random port to the Vite dev server at startup (e.g., 58373). A hardcoded port causes CORS preflight failures for every API request. The fix allows any `localhost` or `127.0.0.1` origin on any port in development.
   - **Behaviour:**
     - **No `CORS:AllowedOrigins` configured (local dev):** Any `localhost` / `127.0.0.1` origin permitted on any port.
     - **`CORS:AllowedOrigins` set (production):** Explicit comma-separated list is enforced; localhost still always allowed for tooling.
   - **Pattern:** `static bool IsLocalOrigin(string origin)` uses `Uri.TryCreate` + pattern match on `uri.Host`. Compatible with `CORS__AllowedOrigins` env var double-underscore convention used in `infra/main.bicep`.
   - **Source:** Ported from PlagueHO/prompt-babbler reference implementation (same stack).
   - **Implementation:** `marginalia-service/src/Api/Program.cs` (lines ~54–80).

1. **Aspire Service Discovery for Frontend API Base URL:** Use Vite's `define` feature to inject the Aspire service URL at build time.
   - **Implementation:**
     - **`vite.config.ts`** reads `process.env.services__api__https__0` (HTTPS preferred) then `services__api__http__0` at build time and exposes the result as the global `__API_BASE_URL__`.
     - **`src/services/api.ts`** declares `__API_BASE_URL__` as an ambient `const` and uses it as `DEFAULT_BASE_URL` when it is defined and non-empty, falling back to `http://localhost:5279`.
   - **Rationale:** Build-time injection (vs runtime fetch) is the correct Vite idiom — the value is tree-shaken and inlined with no runtime config endpoint needed.
   - **Behaviour:**
     - **Running under Aspire:** Frontend automatically uses the Aspire-managed URL → no CORS issues.
     - **Running standalone:** Behaviour unchanged (`http://localhost:5279`). Vite emits `""` for undefined env vars, and the guard in `api.ts` falls through to the hardcoded localhost URL.
   - **Fallback:** `setApiBaseUrl` / `getApiBaseUrl` still available for manual overrides (e.g. in tests or future configuration UI).
   - **Source:** Pattern sourced from the `PlagueHO/prompt-babbler` reference implementation which uses the identical stack.
   - **Implementation:** `marginalia-app/vite.config.ts`, `marginalia-app/src/services/api.ts`.

### Deployment Naming Clarity (2026-03-22)

1. **Rename Model Deployment "foundry" → "reviewer":** The AI model deployment was named `"foundry"`, which conflated with the AI Foundry account (`"ai-foundry"`). Renamed the deployment resource to `"reviewer"` in AppHost.cs and Program.cs, and variable `modelDeployment` → `reviewerDeployment` for consistency.
    - **Rationale:** The deployment is specifically for analyzing and reviewing manuscripts; the new name is self-documenting.
    - **Scope:** Name changes appear in AppHost.cs (orchestration) and Program.cs (`AddChatClient` registration). No breaking changes — deployment name used internally only.
    - **Consequences:** Clearer contributor experience; reduced confusion with account naming.

### Cosmos DB & Multi-Tenancy (2026-03-28)

1. **Cosmos DB Persistence with Preview Emulator:** Replace in-memory ConcurrentDictionary storage with Cosmos DB persistence using the Azure Cosmos DB preview emulator. Follow the PlagueHO/prompt-babbler reference pattern.
    - **Rationale:** In-memory storage doesn't scale and data is lost on process restart. Cosmos DB provides persistent storage with ACID guarantees, multi-tenant data isolation via partition keys, and production-ready scaling.
    - **Implementation:** AppHost: `Aspire.Hosting.Azure.CosmosDB` 13.2.0; Api: `Aspire.Microsoft.Azure.Cosmos` 13.2.0; Infrastructure: `Microsoft.Azure.Cosmos` 3.58.0, `Newtonsoft.Json` 13.0.4.
    - **Database Schema:** Three containers (`documents`, `sessions`, `suggestions`) all partition by `/userId` for multi-tenant isolation.
    - **Repository Pattern:** `CosmosDocumentRepository` and `CosmosSessionRepository` implement IDocumentRepository and ISessionRepository with userId parameter on all methods.
    - **Controllers:** Added `GetUserId(HttpRequest)` helper to extract `X-User-Id` header (defaults to `"_anonymous"`).
    - **Consequences:** Data persists across restarts; multi-tenant isolation enforced; clear upgrade path to Azure Cosmos DB. Requires Docker for local emulator.

### Home Page Feature — API Design & Implementation (2026-03-29)

1. **Document Model Extensions:** Added four fields to `Document`: `Title` (string), `Status` (DocumentStatus), `CreatedAt` (DateTimeOffset), `UpdatedAt` (DateTimeOffset). Records use `required init` with `[JsonPropertyName]`. Backward-compatible with existing Cosmos DB documents via sensible defaults on read (empty title → filename, missing status → Draft or Analyzed based on suggestions, missing timestamps → MinValue).

1. **DocumentStatus Enum:** `Draft` (uploaded/created, never analyzed) and `Analyzed` (at least one analysis pass completed). One-way transition — status never goes backward. Re-analysis keeps Analyzed.

1. **Flat REST Hierarchy:** Documents remain a top-level resource. Sessions are NOT parents of documents. Rationale: users think "show me my manuscripts," not "show me my sessions." A session references documents but does not own them. Current hierarchy: `/api/documents` (list), `/api/documents/upload`, `/api/documents/paste`, `/api/documents/{id}`, `/api/documents/{id}/suggestions`, `/api/documents/{id}/analyze`, `/api/documents/{id}/export`.

1. **DocumentSummary DTO:** Lightweight projection for listing — includes id, title, filename, source, status, createdAt, updatedAt, suggestionCount. Excludes `content` and `suggestions` arrays. Keeps listing response small.

1. **DocumentListResponse Wrapper:** Wraps `DocumentSummary[]` in `{ "documents": [...] }` object (not bare array). Allows adding pagination metadata later without breaking the contract.

1. **Title Generation Rules:** Title is optional on upload/paste. Defaults: upload → `"{createdAt:yyyy-MM-dd HH:mm} - {filename}"`, paste → `"{createdAt:yyyy-MM-dd HH:mm} - Untitled"`. Title is always user-editable after creation (future endpoint).

1. **React Router v7 with BrowserRouter:** Three routes: `/` (HomePage — document listing), `/new` (EditorPage in upload mode), `/editor/:documentId` (EditorPage loading existing document). Navigate with `replace: true` after upload/paste to avoid back-button loops.

1. **HomePage as Standalone Page:** HomePage has its own minimal header (branding only). Full AppHeader (with export, config, "New" button) is editor-only. AppHeader logo is a `<Link to="/">` for navigation home.

1. **Frontend userId Support via X-User-Id Header:** Frontend sends `X-User-Id` header on every API request for multi-tenant support.
    - **Implementation:** Module-level `currentUserId` variable in `api.ts` (defaults to `"_anonymous"`). All fetch wrappers inject header transparently. Exports `setUserId(userId)` and `getUserId()` for future auth integration.
    - **Types:** Document, UserSession, and Suggestion interfaces now include `userId: string` field.
    - **Rationale:** Single source of truth for userId in one module variable. Transparent to services and hooks. Future-proof for auth integration via `setUserId()`.
    - **Consequences:** All API requests carry userId context; backend can enforce strict multi-tenant isolation; ready for MSAL integration.

1. **UserId Multi-Tenancy Test Strategy:** Write comprehensive contract-first tests (43 total) validating multi-tenant data isolation and userId behavior.
    - **Coverage:** 14 tests for Document repository contract, 12 for Session repository contract, 9 for domain model userId defaulting, 8 for controller header extraction and isolation.
    - **Approach:** Test doubles implementing new interface signatures; integration tests using WebApplicationFactory; NoOpChatClient to avoid external dependencies.
    - **Validation:** User isolation enforced (user-bob cannot access user-alice data); proper 404s; whitespace handling; record `with` syntax preserves userId.
    - **Consequences:** All 43 tests pass; InMemory repositories enforce userId isolation to prevent test data leaks; Microsoft.Extensions.AI.Abstractions bumped to 10.2.0.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
