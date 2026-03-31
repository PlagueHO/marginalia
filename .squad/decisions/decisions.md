# Team Decisions

This document consolidates all team decisions made during project development. Each decision is timestamped and attributed to the responsible agent.

---

## User Directives

### BYO Model Support (2026-03-22T00:19:45Z)

**By:** Daniel Scott-Raynsford (via Copilot)
**Category:** Backend Architecture

Users provide a URL to the Microsoft Foundry Models endpoint and an API Key. Can be provided via frontend UI or environment variables (`FOUNDRY_ENDPOINT`, `FOUNDRY_API_KEY`, `FOUNDRY_MODEL_NAME`). Environment variables take precedence at startup.

---

### Project Structure — Prompt-Babbler Pattern (2026-03-22T00:19:45Z)

**By:** Daniel Scott-Raynsford (via Copilot)
**Category:** Project Layout

Structure backend and frontend folders per <https://github.com/PlagueHO/prompt-babbler>. Standard structure for all Daniel's repositories. Enforces Clean Architecture: Domain (zero external deps) → Infrastructure → Api.

---

### shadcn/ui Component Library (2026-03-22T00:19:45Z)

**By:** Daniel Scott-Raynsford (via Copilot)
**Category:** Frontend Tooling

Use shadcn/ui (New York style, Radix UI primitives, TailwindCSS v4) for React components. Enables rapid prototyping with accessible, composable components.

---

## Foundry Proxy Pattern (2026-03-22)

### Backend Proxy for All Foundry Calls

**Author:** Gilfoyle (Backend Dev)  
**Status:** Implemented

All Foundry calls route through the backend. Frontend never calls Foundry directly (CORS blocks it).

1. **Frontend Config Push:** Frontend sends Foundry config (endpoint URL, API key, model) to `POST /api/config/llm`
1. **Proxy Analysis:** `POST /api/documents/{id}/analyze` triggers backend to call Foundry via `FoundrySuggestionService`
1. **Foundry URL Format:** `{endpoint}/openai/deployments/{modelName}/chat/completions?api-version=2025-04-01-preview`
1. **Auth Header:** `api-key: {value}` (Foundry project endpoints require this, not Bearer token)
1. **Dual-Path Architecture:** `FoundrySuggestionService` uses `IChatClient` (Aspire) or `HttpClient` (standalone/BYO)

**Consequences:**

- Frontend is fully decoupled from LLM provider
- API key stays server-side after initial config push
- Swapping Foundry for another provider requires zero frontend changes

---

### Architectural Decisions

### Project Structure Scaffolding (2026-03-22)

**Author:** Richard (Lead/Architect)
**Status:** Implemented

1. **Clean Architecture Enforced:** Domain project has zero NuGet dependencies. Only `System.Text.Json` attributes. All external deps in Infrastructure.
1. **Central Package Management:** All versions pinned in `Directory.Packages.props`. Individual csproj files reference without versions.
1. **BYO Model Pattern:** `LlmEndpointOptions` supports both environment variables and user-provided config. Env vars take precedence.
1. **Domain Model Contracts:** All models are `sealed record` with `required init` properties and `[JsonPropertyName("camelCase")]`. Enums serialize as strings.
1. **Frontend Toolchain:** React 19 + Vite 8 + TypeScript strict + TailwindCSS v4 + shadcn/ui (New York) + Vitest.
1. **Path Alias:** `@/*` maps to `./src/*` in TypeScript and Vite resolve config.

**Impacts:**

- Dinesh (Frontend): marginalia-app/ ready for component addition
- Gilfoyle (Backend): marginalia-service/ ready for DI wiring
- Jared (Testing): Test projects wired and ready

---

### Backend Implementation (2026-03-22)

**Author:** Gilfoyle (Backend Dev)
**Status:** Implemented

1. **In-Memory Storage Only:** No database. `ConcurrentDictionary` in singleton repositories. Matches spec requirement for local/session-based persistence.
1. **IOptionsMonitor for LLM Config:** Uses `IOptionsMonitor<LlmEndpointOptions>` instead of `IOptions` for runtime config updates without restart. Env vars take precedence on startup.
1. **Document Chunking Strategy:** ~6000 characters per chunk (~3 pages). Breaks on paragraph boundaries (`\n\n`), falls back to line breaks. Character offsets tracked for suggestion TextRange mapping.
1. **DocumentFormat.OpenXml for Word:** Added DocumentFormat.OpenXml 3.3.0 for .docx parsing/export. Type alias pattern (`DomainDocument`) resolves namespace collision.
1. **Source-Generated JSON Serialization:** `FoundrySerializerContext` for Foundry API DTOs. Avoids reflection, supports trimming/AOT.
1. **CORS Policy:** Default allows `http://localhost:5173` (Vite dev server). Any header, any method. Production requires tightening.
1. **API Route Convention:** All routes under `/api/` prefix. Controllers use `[Route("api/[controller]")]`.
1. **Export Applies Accepted Suggestions:** `/api/documents/{id}/export` applies all accepted suggestions before generating .docx. Applied in reverse offset order.

**Open Questions:**

- Google Docs integration not implemented — needs OAuth flow decision
- No rate limiting on analyze endpoint — could be expensive with large docs
- Session-to-document association exists but not enforced in upload flow

---

### Aspire Orchestration Layer (2026-03-22)

**Author:** Gilfoyle (Backend Dev)
**Status:** Implemented

Adopted .NET Aspire 13.1.3 as orchestration layer following prompt-babbler reference pattern:

1. **AppHost** orchestrates API + Vite frontend + Azure AI Foundry with chat deployment
1. **ServiceDefaults** provides shared infrastructure (OpenTelemetry, health checks, resilience, service discovery)
1. **IChatClient** from `Microsoft.Extensions.AI` injected via Aspire when connection string `ai-foundry` present
1. **Dual-Path AI Calls:** `FoundrySuggestionService` uses `IChatClient` when Aspire available, falls back to `HttpClient` + env vars for standalone/BYO
1. **No Cosmos DB** — in-memory storage per spec
1. **No Auth** — not in spec

**Consequences:**

- Developers start via AppHost for full dashboard, tracing, health checks
- BYO model pattern still works standalone (env vars)
- Integration tests reference AppHost for orchestration verification
- Solution `defaultStartup` is the AppHost project

---

### Test Strategy and Coverage Standards (2026-03-22)

**Author:** Jared (Tester)
**Status:** Proposed / Implemented

1. **Test Double Strategy for Repositories:** Use contract-based test doubles (e.g., `TestDocumentRepository` with `ConcurrentDictionary`) instead of NSubstitute mocks. Concrete doubles validate behavioral contracts; mocking only tests the mock.
1. **Accessibility Testing with Exclusions:** Axe tests for all UI components. Three components (DocumentUploader, SuggestionCard, SuggestionPanel) have known `nested-interactive` violations — buttons nested in focusable regions. Tests exclude with documenting comment.
1. **Frontend Mock Strategy:** Mock `fetch` globally (via `vi.stubGlobal`) not individual service modules. Tests full request chain.
1. **Test Naming Conventions:**
   - Backend: `MethodName_Scenario_ExpectedResult` with `[TestCategory("Unit")]`
   - Frontend: `describe/it` blocks organized by behavior group
1. **jest-axe Setup:** Added `import 'jest-axe/extend-expect'` to `vitest.setup.ts` for global `toHaveNoViolations()` matcher.

**Current Status:**

- 79 backend tests passing
- 88 frontend tests passing
- Total 167 tests covering domain models, service contracts, UI components, API client behavior

**Open Items:**

- Thread safety tests for InMemoryDocumentRepository when implementation lands
- Integration tests pending API controller implementation
- End-to-end tests after full stack running

---

### Aspire Orchestration Integration Test Strategy (2026-03-22)

**Author:** Jared (Tester)
**Status:** Implemented

1. **Test Scope:** Orchestration integration tests verify Aspire app model only — resource existence, naming, relationships. Do NOT make HTTP calls (requires Azure credentials, avoided by design).
1. **Test Pattern:** Use `DistributedApplicationTestingBuilder.CreateAsync<Projects.Marginalia_AppHost>()` to build app model, then query `DistributedApplicationModel.Resources` via DI.
1. **Project SDK:** Uses `MSTest.Sdk/4.1.0` (not Microsoft.NET.Sdk) to match prompt-babbler reference pattern.
1. **Location:** `tests/integration/Orchestration.IntegrationTests/` — separate from existing API integration tests.
1. **Package Versions:** `Aspire.Hosting.Testing` (9.2.0) and `coverlet.collector` (6.0.4) in central `Directory.Packages.props`.

**Implications:**

- Tests run in CI without Azure credentials
- New Aspire resources require corresponding test assertions
- Separate test category for HTTP service behavior verification (if needed later)

---

## Frontend Implementation (2026-03-22)

**Author:** Dinesh (Frontend Dev)
**Status:** Implemented

1. **State Management:** Custom hooks (no Redux/Zustand). At current scale, clean separation without external overhead. Reconsider if multi-page or broader cross-component sharing needed.
1. **API Service Layer:** All calls through `src/services/api.ts` (`apiGet`, `apiPost`, `apiPut`, `apiPostFile`, `apiGetBlob`). Base URL `http://localhost:5279`, configurable via `setApiBaseUrl()`.
1. **Expected Backend Endpoints:**
   - POST /api/documents/upload
   - POST /api/documents/paste
   - GET /api/documents/:id
   - POST /api/documents/analyze
   - GET /api/documents/:id/export
   - GET /api/documents/:id/suggestions
   - PUT /api/documents/:id/suggestions/:id
   - GET/PUT /api/config/llm
   - POST /api/config/llm/test
   - POST/GET /api/sessions/:id
1. **Suggestion Highlight Accessibility:** Both color AND status icons. Focusable, ARIA labels for colorblind support.
1. **shadcn/ui Sonner:** Use `sonner` not deprecated `toast`. Stripped `next-themes` dependency.
1. **Dark Mode:** CSS variables via `@media (prefers-color-scheme: dark)` in index.css. System preference, no toggle yet.

---

## Config Endpoint — IChatClient Metadata Fix (2026-03-22T08:39:00Z)

**Author:** Gilfoyle  
**Status:** Implemented ✅

`GET /api/config/llm` was returning placeholder values from `appsettings.Development.json` (e.g., `https://your-foundry-account...`, `gpt-4o`) instead of the real endpoint and model name the Aspire-managed `IChatClient` connects to.

**Root Cause:** Two disconnected config paths — `LlmEndpointOptions` (appsettings) vs. Aspire's `ai-foundry` connection string. The controller read from the former; the actual client used the latter.

**Decision:**

- `ConfigController.GetLlmConfig()` calls `_chatClient?.GetService<ChatClientMetadata>()` to extract `ProviderUri` and `DefaultModelId` from the live client
- Falls back to `LlmEndpointOptions` when no metadata available (standalone/non-Aspire scenarios)
- Removed placeholder endpoint/model and stale `ApiKey` fields from `appsettings.Development.json` and `appsettings.json`

**Rationale:**

- No AppHost changes — bug is in the API service, not orchestration
- No hardcoded values — metadata comes from the actual client instance
- Backward compatible — `LlmEndpointOptions` fallback preserves non-Aspire path
- Zero new dependencies — `ChatClientMetadata` is part of `Microsoft.Extensions.AI.Abstractions` already referenced

**Consequences:**

- Frontend Model Configuration dialog now shows actual endpoint and model name when running under Aspire
- `LlmEndpointOptions` remains available for standalone configuration via env vars (`FOUNDRY_ENDPOINT`, `FOUNDRY_MODEL_NAME`)
- Build: 0 errors, 78 tests passing

---

## Key Learnings Captured

### Domain Models

Domain models are records with no validation. TextRange, Document, Suggestion accept invalid values. Validation lives in service/API layer.

### NSubstitute Patterns

When mixing `null` with `Arg.Any<>` matchers, use `Arg.Any<string?>()` for all args of same type. Never mix literal `null` with `Arg.Any<>`.

### Test Infrastructure

- Backend: MSTest + NSubstitute + FluentAssertions (via MSTest.Sdk)
- Frontend: Vitest + @testing-library/react + jest-axe
- Both parallelize by default
- `jest-axe/extend-expect` must import in `vitest.setup.ts`

### Suggestion Serialization

`[JsonConverter(typeof(JsonStringEnumConverter))]` ensures `"Pending"` not `0`. Frontend types match with string union `'Pending' | 'Accepted' | 'Rejected' | 'Modified'`.

### Aspire Testing Pattern

Use `DistributedApplicationTestingBuilder` to build app model. Verify resources via `DistributedApplicationModel.Resources` from DI. Don't make HTTP calls. Tests validate wiring, not running services. Uses `MSTest.Sdk/4.1.0`, `Aspire.Hosting.Testing`, FluentAssertions.

### Aspire Test Project Conventions

Orchestration integration tests in `tests/integration/Orchestration.IntegrationTests/`. Namespace `Marginalia.Orchestration.IntegrationTests`. ProjectReference to AppHost enables `Projects.Marginalia_AppHost` generated type. Central package management pins `Aspire.Hosting.Testing` and `coverlet.collector`.

---

### User Directive — Backend Proxy for Foundry (2026-03-22T03:31:00Z)

**By:** Daniel Scott-Raynsford (via Copilot)  
**Category:** Architecture

Backend must proxy all AI/Foundry calls. Frontend never calls Foundry directly (CORS blocks it). Frontend pushes Foundry config (endpoint URL, API key, model name) to the backend config endpoint, and the backend makes the actual Foundry API calls.

**Rationale:** Microsoft Foundry endpoints don't support CORS. Direct browser-to-Foundry calls fail. Backend proxy is the correct architecture.

---

## Architecture Decisions (Sprint 2026-03-22)

### Entra-Only Authentication for Azure AI Foundry (2026-03-22T07:20:00Z)

**Author:** Gilfoyle  
**Status:** Implemented ✅

Remove all API key authentication paths for Azure AI Foundry. The backend now authenticates exclusively via **Entra ID (DefaultAzureCredential)** through the Aspire Azure AI Inference client integration (`Aspire.Azure.AI.Inference`).

**Context:** The previous implementation supported three auth paths (API key via OpenAI SDK, Entra ID fallback via `AzureOpenAIClient`, Aspire-provided `IChatClient`). This added complexity, surface area for credential leakage, and required unnecessary NuGet packages.

**Decision:**

- **Frontend** can no longer POST/PUT `/api/config/llm` — that endpoint is removed. No credentials are ever sent from the browser.
- **Backend** registers `IChatClient` exclusively via `builder.AddAzureChatCompletionsClient("ai-foundry").AddChatClient("chat")` — the Aspire integration uses DefaultAzureCredential automatically.
- **Analysis endpoints** fail with a DI exception if `IChatClient` is not registered (i.e., not running under Aspire). This is intentional — Aspire is the required runtime.
- **New health endpoint** `GET /api/config/llm/health` returns `{ healthy, message }` so the frontend can check connectivity without spending tokens.
- **Removed packages:** `Aspire.Azure.AI.OpenAI`, `Azure.AI.OpenAI`, `OpenAI`
- **Added package:** `Aspire.Azure.AI.Inference` 13.1.3-preview.1.26166.8

**Trade-offs:**

- **Loses:** Standalone mode (running without Aspire). Previously users could supply an API key and run without Aspire orchestration.
- **Gains:** Simpler codebase, no credential handling, no masked API key storage, smaller dependency footprint.

**Consequences:** Backend now exclusively authenticates to Azure AI Foundry via Entra ID through Aspire. Requires Aspire runtime.

---

### Config Dialog Becomes Readonly Health Check View (2026-03-22T07:20:00Z)

**Author:** Dinesh  
**Status:** Implemented ✅

Configuration is fully backend-owned. The frontend config dialog is readonly.

**Context:** The LLM configuration was previously editable from the frontend — users could set endpoint URL, API key, and model name via the config dialog, and the frontend would POST to `/api/config/llm`. Gilfoyle's backend now fully owns configuration via Aspire environment variables/appsettings, and always uses Entra ID authentication. There is no longer a POST endpoint for config.

**Decision:**

- `LlmConfigDialog` no longer contains any `<Input>` elements. Endpoint URL and model name are displayed as styled readonly text.
- API key concept eliminated from the frontend entirely (`apiKey` removed from `LlmConfig` type).
- Auth method is always "Entra ID" — always shown as a fixed badge (ShieldCheck icon).
- "Test Connection" and "Save" buttons replaced by a single "Check Connection" button that calls `GET /api/config/llm/health`.
- New `LlmHealthResult` type `{ healthy: boolean; message: string }` used for health status display.
- Green CheckCircle2 for healthy, red XCircle for unhealthy, Loader2 spinner while checking.

**Consequences:**

- Frontend never modifies backend config. Zero chance of frontend accidentally overwriting Aspire-managed settings.
- `configService` is now read-only: only `getLlmConfig` and `checkHealth` remain.
- `useLlmConfig` hook no longer exposes `updateConfig`, `testConnection`, or `setLocalConfig`.
- `AppHeader` and `EditorPage` are simpler — three props and two callbacks removed.

**API Contract (Gilfoyle):**

- `GET /api/config/llm` → `{ endpoint, modelName, isConfigured, authMethod }`
- `GET /api/config/llm/health` → `{ healthy: boolean, message: string }`
- `POST /api/config/llm` — **REMOVED**

---

### Response Envelope for Upload and Paste Endpoints (2026-03-22T09:10:00Z)

**Author:** Gilfoyle  
**Status:** Implemented ✅

Both `/api/documents/upload` and `/api/documents/{id}/paste` endpoints now wrap responses in `UploadDocumentResponse` record.

**Context:** Frontend `useDocument` hook expected `{ document, sessionId }` structure from both endpoints, but the backend returned raw `Document` objects. This caused `response.document` to be undefined, triggering a deserialization error caught as "Failed to process text".

**Decision:**

- Added `UploadDocumentResponse` record: `record UploadDocumentResponse(Document Document, string SessionId)` in `src/Domain/Models/`
- Both `Upload` and `Paste` endpoints now create a `UserSession` with generated GUID `SessionId`, persist via `ISessionRepository.SaveAsync()`, and return `new UploadDocumentResponse(document, sessionId)`
- Added `[RequestSizeLimit(52_428_800)]` (50 MB) to `Paste` endpoint to match `Upload` (was missing)
- Response wrapped in `CreatedAtAction` per REST convention

**Consequences:**

- Frontend receives correct `{ document, sessionId }` structure — no more undefined access errors
- Session infrastructure now activated at document creation time
- Every upload/paste creates a new session (multi-document session grouping would require different flow, out of scope)
- Zero frontend changes required — `useDocument` hook already expects this shape
- 78 unit tests pass unchanged

**API Contract:**

- `POST /api/documents/upload` → `{ document: Document, sessionId: string }`
- `POST /api/documents/{id}/paste` → `{ document: Document, sessionId: string }`

---

**Last Updated:** 2026-03-22T09:10:00Z  
**Maintained By:** Scribe

---

## Analyze Endpoint URL and Response Type Fix (2026-03-22T09:27:00Z)

**Author:** Dinesh (Frontend Dev)  
**Status:** Accepted ✅

### Context

The `analyzeDocument()` service function had two bugs that together caused a 405 error and a broken response when analysis was triggered from the editor.

### Decisions

#### 1. Analyze URL must include document ID in path

`documentService.ts` must POST to `/api/documents/{documentId}/analyze` (not `/api/documents/analyze`). The backend route is `[HttpPost("{id}/analyze")]`, so omitting the ID segment caused the router to match `[HttpGet("{id}")]` with `id = "analyze"` — a GET endpoint — resulting in a 405 Method Not Allowed.

#### 2. Analyze response is a JSON array, not a wrapper object

The backend's analyze endpoint returns `Ok(suggestions)` where `suggestions` is `IReadOnlyList<Suggestion>`, which serializes directly as a JSON array `[...]`. The frontend must treat the response as `Suggestion[]`, not `{ suggestions: Suggestion[] }`.

#### 3. Remove `AnalyzeResponse` type

The `AnalyzeResponse` interface (`{ suggestions: Suggestion[] }`) was based on an incorrect assumption about the response envelope. It has been removed from `types/api.ts` and `types/index.ts`. `documentService.ts` imports `Suggestion` directly from `@/types`.

### Consequences

- Analyze requests will reach the correct backend route and receive a valid HTTP response.
- `useAnalysis.ts` returns the array directly — callers (EditorPage) are unaffected since the hook's public API (`Promise<Suggestion[]>`) is unchanged.
- Removing `AnalyzeResponse` eliminates a misleading type from the codebase.
