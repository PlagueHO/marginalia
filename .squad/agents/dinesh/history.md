# Project Context

- **Owner:** Mary MacGregor-Reid
- **Project:** Marginalia — AI-powered narrative flow editor for long-form non-fiction writers. Helps authors refine, expand, and ensure stylistic consistency in manuscripts with AI-generated suggestions and fine-grained editorial control.
- **Stack:** React (frontend), .NET 10 / ASP.NET (backend), Azure Functions, Azure AI / Microsoft Foundry, Google Docs API integration
- **Created:** 2026-03-22

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### Analyze Endpoint URL & Response Type Fix (2026-03-22)

**Status:** ✅ COMPLETE — Build clean (0 errors, 0 warnings from tsc).

**Two bugs fixed:**

1. **Wrong URL (405 Method Not Allowed):** `documentService.ts` was posting to `/api/documents/analyze` (a flat path with no `{id}` segment). The backend route is `POST /api/documents/{id}/analyze`. Fixed to use `` `/api/documents/${request.documentId}/analyze` ``.

1. **Response type mismatch:** The backend's analyze endpoint returns `Ok(suggestions)` which serializes as a JSON array `[...]`. The frontend declared return type as `Promise<AnalyzeResponse>` (a wrapper `{ suggestions: Suggestion[] }`), so `response.suggestions` was always `undefined`. Fixed `analyzeDocument()` to return `Promise<Suggestion[]>` and updated `useAnalysis.ts` to use `response` directly (not `response.suggestions`).

**`AnalyzeResponse` type removed** — it was only used for the now-corrected wrapping assumption. Removed from `api.ts` and the re-export in `index.ts`. `documentService.ts` now imports `Suggestion` directly.

**Key files:**

- `marginalia-app/src/services/documentService.ts`
- `marginalia-app/src/hooks/useAnalysis.ts`
- `marginalia-app/src/types/api.ts`
- `marginalia-app/src/types/index.ts`

### Home Page, React Router & Title Support (2026-03-29)

**Status:** ✅ COMPLETE — Build clean (tsc 0 errors), 82 tests pass (2 pre-existing failures in api.test.ts unrelated to this work).

**What was built:**

1. **React Router (v7.13.2):** Routes `/` (Home), `/new` (upload), `/editor/:documentId` (editor). BrowserRouter in App.tsx.
1. **HomePage (`pages/HomePage.tsx`):** Lists manuscripts from `GET /api/documents`. Shows title, status badge (Draft/Analyzed), date, suggestion count. Empty state with "No manuscripts yet" message.
1. **useDocuments hook (`hooks/useDocuments.ts`):** `{ documents, isLoading, error, loadDocuments }` — follows existing hook pattern.
1. **DocumentSummary type:** Added to `types/document.ts` along with `DocumentStatus` and `DocumentListResponse` in `types/api.ts`.
1. **Document model updated:** Added `title`, `status`, `createdAt`, `updatedAt` fields.
1. **DocumentUploader title input:** Optional text input above the upload area. Title flows through to `uploadDocument(file, title)` and `pasteDocument({ content, filename, title })`.
1. **apiPostFile extended:** Now accepts optional `extraFields` record for additional FormData fields (used for title on upload).
1. **EditorPage routing:** Uses `useParams` for documentId, loads existing doc on mount, navigates to `/editor/{id}` after upload/paste (replace: true to avoid back-button loops). "New" button navigates to `/`.
1. **AppHeader home link:** Marginalia logo is now a `<Link to="/">`.
1. **listDocuments service:** Added `apiGet<DocumentListResponse>('/api/documents')`.

**Key files changed:**

- `marginalia-app/src/App.tsx` — BrowserRouter + Routes
- `marginalia-app/src/pages/HomePage.tsx` — NEW
- `marginalia-app/src/pages/EditorPage.tsx` — react-router params + navigation
- `marginalia-app/src/hooks/useDocuments.ts` — NEW
- `marginalia-app/src/hooks/useDocument.ts` — title params on upload/paste
- `marginalia-app/src/components/DocumentUploader.tsx` — title input
- `marginalia-app/src/components/AppHeader.tsx` — Home link
- `marginalia-app/src/types/document.ts` — DocumentStatus, DocumentSummary, Document fields
- `marginalia-app/src/types/api.ts` — DocumentListResponse, PasteRequest.title
- `marginalia-app/src/types/index.ts` — re-exports
- `marginalia-app/src/services/documentService.ts` — listDocuments, title params
- `marginalia-app/src/services/api.ts` — apiPostFile extraFields
- `marginalia-app/src/services/index.ts` — export listDocuments
- `marginalia-app/tests/components/DocumentUploader.test.tsx` — updated assertions for new signatures

**Design decisions:**

- HomePage has its own minimal header (not full AppHeader) since it doesn't need editor controls.
- Title is optional — backend generates default per Richard's spec.
- `navigate(replace: true)` after upload/paste prevents back-button loops.
- Home button goes to `/` (full manuscript list) not just state-clear.

### 2026-03-29 — Cross-Agent: Home Page Feature Complete

**Richard (Lead):** API contracts designed and approved. Flat REST hierarchy, DocumentSummary DTO, title defaults.

**Gilfoyle (Backend):** All backend endpoints implemented matching API design. Build clean, 163 tests pass. Legacy Cosmos documents handled with sensible defaults.

**Jared (Tester):** Frontend tests for HomePage, useDocuments, and DocumentUploader title written. All pass against Dinesh's implementation.

### Aspire Service Discovery — Frontend API Base URL (2026-03-22)

**Status:** ✅ COMPLETE — Build clean, 84 tests pass.

**Summary:** Wired Aspire service discovery env vars into the Vite build so the frontend uses the Aspire-managed API URL when running under Aspire, and falls back to `http://localhost:5279` when running standalone.

**Changes:**

- `marginalia-app/vite.config.ts` — Added `__API_BASE_URL__` via Vite `define`, reading `process.env.services__api__https__0` (prefer HTTPS) then `services__api__http__0`, defaulting to empty string if neither is set.
- `marginalia-app/src/services/api.ts` — Added `declare const __API_BASE_URL__: string` ambient declaration; `DEFAULT_BASE_URL` now uses `__API_BASE_URL__` when it is defined and non-empty, otherwise falls back to `http://localhost:5279`.

**Pattern (from prompt-babbler reference):**

- Aspire injects `services__{name}__{scheme}__{index}` env vars for non-.NET resources via `WithReference()`.
- Vite's `define` feature replaces `__API_BASE_URL__` at build time — the TypeScript `declare const` is required to satisfy the compiler without a type error.
- Fallback guard (`typeof __API_BASE_URL__ !== 'undefined' && __API_BASE_URL__ !== ''`) handles local dev where `define` emits `""`.

**Key files:**

- `marginalia-app/vite.config.ts`
- `marginalia-app/src/services/api.ts`

**Decision logged:** `.squad/decisions/inbox/dinesh-aspire-api-discovery.md` → merged to decisions.md 2026-03-22T08:07:46Z

**Orchestration Log:** `.squad/orchestration-log/2026-03-22T08_07_46Z-dinesh.md`

### Readonly Config / Health Check Refactor — Final Implementation (2026-03-22T07:20:00Z)

**Status:** ✅ COMPLETE — Build clean, 84 tests pass.

**Summary:** Config dialog fully converted from editable form to readonly status view. Configuration now exclusively backend-owned via Aspire environment variables. Frontend never sends credentials or modifies config.

**Architectural Shift:**

- `LlmConfigDialog` is a pure status display — no input fields, no forms.
- `apiKey` completely removed from `LlmConfig` type. `authMethod` is always `"entraId"`.
- New `LlmHealthResult` type replaces `LlmConfigTestResult`: `{ healthy: boolean; message: string }`.
- "Test Connection" + "Save" buttons replaced by single "Check Connection" button.
- Health status display: Spinner (loading), CheckCircle2 (healthy), XCircle (unhealthy).

**Service/Hook Simplification:**

- `configService.ts` now exports only `getLlmConfig` (GET) and `checkHealth` (GET to `/api/config/llm/health`).
- Removed: `updateLlmConfig`, `testLlmConnection`, all write operations.
- `useLlmConfig` hook exposes: `{ config, isLoading, isCheckingHealth, healthResult, error, loadConfig, checkHealth }`.
- Removed: `updateConfig`, `testConnection`, `setLocalConfig`, and related state.

**Component Updates:**

- `LlmConfigDialog.tsx` — Full rewrite: readonly display layout, Entra ID badge (ShieldCheck) always visible, health check button.
- `AppHeader.tsx` — Simplified props: removed `onSaveConfig`, `onTestConfig`, `onConfigChange`.
- `EditorPage.tsx` — `handleCheckHealth` replaces `handleSaveConfig` and `handleTestConfig`.
- `LlmConfigDialog.test.tsx` — Full rewrite matching new readonly UI; tests badge visibility, health button, no input elements.

**Type Changes:**

- `src/types/api.ts` — removed `apiKey` from `LlmConfig`; removed `LlmConfigTestResult`; added `LlmHealthResult`.
- `LlmConfig` now: `{ endpoint: string; modelName: string; isConfigured: boolean; authMethod: string }`.

**Key Learnings:**

- `testing-library` `getByText` throws on multiple element matches — use `getByTestId` or `getAllByText` when text appears in composite elements (badge + description).
- `toHaveTextContent` on container is correct tool for status displays spanning multiple inline `<span>` elements.
- Readonly design eliminates form validation, state synchronization, and credential handling complexity.

**Orchestration Log:** `.squad/orchestration-log/2026-03-22T07_20_00Z-dinesh.md`
**Decision:** `.squad/decisions/decisions.md` → Config Dialog Becomes Readonly Health Check View section

### Frontend Implementation (2025-07-22)

**Architecture:**

- Component-first architecture: types → services → hooks → components → pages
- State managed via custom hooks (`useDocument`, `useSuggestions`, `useAnalysis`, `useLlmConfig`) — no global state library needed at this scale
- API service layer uses a thin fetch wrapper (`services/api.ts`) with configurable base URL (default `http://localhost:5279`)
- shadcn/ui (New York style) for all primitives; sonner for toast notifications

**Key files:**

- `src/types/` — TypeScript types mirroring backend C# models (Document, Suggestion, TextRange, SuggestionStatus, UserSession, LlmConfig)
- `src/services/` — API client (api.ts), documentService, suggestionService, configService, sessionService
- `src/hooks/` — useDocument, useSuggestions, useAnalysis, useLlmConfig
- `src/components/` — DocumentUploader, DocumentEditor, DocumentHeader, SuggestionHighlight, SuggestionCard, SuggestionPanel, SuggestionBatchActions, AnalysisControls, LlmConfigDialog, ExportControls, AppHeader, MainLayout
- `src/pages/EditorPage.tsx` — main application page composing all components
- `src/App.tsx` — root with TooltipProvider and Toaster

**Patterns:**

- Suggestion highlights use both color AND icons (not color-only) for accessibility: AlertCircle (pending/amber), Check (accepted/green), X (rejected/red), Pencil (modified/blue)
- Document editor segments content into text/suggestion spans sorted by position — handles overlapping ranges
- `SuggestionPanel` uses shadcn Tabs for filtering by status
- Layout: 65% editor + 35% suggestion panel on desktop, stacked on mobile
- shadcn CLI installed components into literal `@/` directory — had to manually move to `src/components/ui/`. The sonner component also imported from itself (circular) and used `next-themes` — had to fix both.

**Gotchas:**

- shadcn@latest CLI resolves `@/` alias literally when run outside a Next.js project — always verify output path
- sonner component ships with `next-themes` dependency — strip it for vanilla Vite+React projects
- TypeScript strict mode with `noUnusedLocals`/`noUnusedParameters` requires explicit event handler types (e.g., `React.ChangeEvent<HTMLInputElement>`)

### Team Update — Axe Accessibility Testing (2026-03-22)

**From Jared's test suite:** 3 nested-interactive axe violations identified in SuggestionCard and DocumentUploader components. Both have interactive elements (buttons) nested inside focusable containers, which violates WCAG accessibility standards. This is a common pattern with shadcn/ui card-based UIs.

**Recommended resolution:** Move action buttons outside the focusable card header or use aria-owns pattern. Consider placing button groups in a separate non-focusable area of the card footer or moving them outside the card entirely.

### Team Update — Foundry Proxy Fix Session (2026-03-22T03:35:00Z)

**From Gilfoyle's work:** Backend `FoundrySuggestionService` fixed to use `/openai/deployments/{model}/chat/completions` URL format and `api-key` header. ConfigController now accepts POST to `/api/config/llm`. Build zero warnings, 79 tests pass.

**Coordination Note:** Frontend config POST and backend Foundry HTTP now correctly aligned. The proxy pattern is complete: frontend → backend config (POST) → backend analysis (POST) → Foundry HTTP.

**Problem:** CORS prevents the frontend from calling Foundry directly. All AI calls must go through the backend proxy.

**Changes:**

- `configService.ts`: Changed `updateLlmConfig` from `apiPut` to `apiPost` to match the backend's new `POST /api/config/llm` endpoint contract.
- `useLlmConfig.ts`: `updateConfig` now re-throws errors so callers (EditorPage) can distinguish success from failure and show appropriate toasts.
- `EditorPage.tsx`: `handleSaveConfig` wrapped in try/catch — shows `toast.success` on save, `toast.error` on failure.
- Test updated to assert POST method for config save.

**Verification:** The frontend already had no direct Foundry calls — all HTTP goes through `api.ts` to `localhost:5279`. The only functional change was PUT→POST for config save and proper error feedback. Build clean, all 88 tests pass.

### OpenTelemetry Browser SDK (2026-03-22)

**Status:** ✅ COMPLETE — Build clean, 84 tests pass.

**Summary:** Added full OpenTelemetry browser instrumentation (traces + metrics) matching the prompt-babbler reference. Telemetry gracefully no-ops when the OTLP endpoint is absent (standalone dev).

**Changes:**

- `marginalia-app/package.json` — Added 13 `@opentelemetry/*` dependencies (api, context-zone, exporter-metrics-otlp-http, exporter-trace-otlp-proto, instrumentation, instrumentation-document-load, instrumentation-fetch, instrumentation-user-interaction, resources, sdk-metrics, sdk-trace-base, sdk-trace-web, semantic-conventions).
- `marginalia-app/src/telemetry.ts` — NEW FILE. Initializes WebTracerProvider + MeterProvider with OTLP exporters. Auto-instruments document load, fetch, and user interactions. Exports `tracer`, `meter`, and `endSpanWithDuration` helper for custom spans.
- `marginalia-app/src/main.tsx` — Calls `initTelemetry()` before React render.
- `marginalia-app/vite.config.ts` — Forwards `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_HEADERS`, `OTEL_RESOURCE_ATTRIBUTES`, `OTEL_SERVICE_NAME` env vars via Vite `define` (same pattern as `__API_BASE_URL__`).

**Pattern:**

- Aspire injects OTEL env vars when the dashboard is running. Vite's `define` replaces `__OTEL_*__` globals at build time.
- `typeof __VAR__ !== 'undefined'` guard handles standalone dev where define emits `""`.
- `initTelemetry()` checks for endpoint presence and skips all SDK setup if absent — zero runtime cost in standalone mode.

**Key files:**

- `marginalia-app/src/telemetry.ts`
- `marginalia-app/src/main.tsx`
- `marginalia-app/vite.config.ts`

### Entra ID Authentication UI Support (2026-03-22)

**Status:** ✅ COMPLETE (Build clean, 88 tests pass)

**Context:** Backend adding Entra ID (Azure CLI credential) fallback when no API key is provided. Frontend updated to reflect optional API key and show auth method indicator.

**Changes:**

- `src/types/api.ts`: Added `authMethod?: string` and `isConfigured?: boolean` to `LlmConfig` interface for backend compatibility.
- `src/components/LlmConfigDialog.tsx`:
  - API Key label changed to "API Key (Optional)" with `aria-describedby` helper text explaining Entra ID fallback.
  - Placeholder updated to clarify optional nature.
  - Auth method indicator using shadcn Badge (secondary for API Key, outline for Entra ID) with lucide-react icons (KeyRound, ShieldCheck).
  - Indicator only shows when endpoint is filled — conditionally renders based on `localConfig.endpoint`.
  - DialogDescription updated to mention Entra ID fallback.
  - Test Connection button already correctly gated on `!localConfig.endpoint` — no API key required.

**Verification:** Build clean, all 88 existing tests pass. No test changes needed — existing regex matchers (`/api key/i`) still match updated label text.

**Team Coordination:**

- Gilfoyle implemented dual-path auth (API key + Entra ID) with `DefaultAzureCredential`
- Orchestration log: `.squad/orchestration-log/2026-03-22T04_18_15Z-dinesh.md`

### OpenTelemetry Browser SDK (2026-03-22T08:15:00Z)

**Status:** ✅ COMPLETE — Build 0 warnings/errors, 84 unit tests pass.

**Outcome Verification:**

- Orchestration log: `.squad/orchestration-log/2026-03-22T08_15_00Z-dinesh.md`
- Decision merged: `.squad/decisions.md` → OpenTelemetry Browser SDK section
- 13 new `@opentelemetry/*` packages integrated and bundled
- Telemetry initialization in place; all auto-instrumentations operational
- Bundle size increase ~200KB gzipped (OTel SDK + zone.js)
- No-op path verified: standalone `pnpm dev` unaffected (zero cost when OTLP endpoint absent)

**Integration with Telemetry Stack:**

- Backend structured logging (Gilfoyle) + Frontend OTel SDK (this work) provide end-to-end tracing
- Session log: `.squad/log/2026-03-22T08_15_00Z-telemetry-improvement.md`
- Next: Verify trace correlation in Aspire dashboard; consider custom spans in FoundrySuggestionService for chunk-level visibility

### Team Update — Paste/Upload Response Fix (2026-03-22T09:10:00Z)

**From Gilfoyle's work:** Backend `DocumentsController` now wraps both upload and paste endpoint responses in `{ document, sessionId }` envelope. Fixes "Failed to process text" deserialization error on frontend. Build clean, 78 tests pass.

**Impact on Frontend:** `useDocument` hook already receives correct response shape from both endpoints. API contract now fully aligned.

### userId Multi-Tenant Support — Frontend API Layer (2026-03-22)

**Status:** ✅ COMPLETE — Build clean, TypeScript compilation successful.

**Context:** Backend updated (by Gilfoyle) to persist all data to Cosmos DB with userId partition key, defaulting to `"_anonymous"`. Backend extracts userId from `X-User-Id` HTTP header. Frontend wired up to send this header on all API requests.

**Changes:**

1. **Type Updates** — Added `userId: string` field to all domain types:
   - `src/types/document.ts` — Added `userId` to `Document` interface
   - `src/types/session.ts` — Added `userId` to `UserSession` interface
   - `src/types/suggestion.ts` — Added `userId` to `Suggestion` interface

1. **API Service** — `src/services/api.ts`:
   - Module-level `currentUserId` variable initialized to `"_anonymous"`
   - Exported `setUserId(userId: string)` and `getUserId()` functions for future auth integration
   - Added `X-User-Id: currentUserId` header to ALL fetch wrapper functions:
     - `apiGet` — Added to headers object
     - `apiPost` — Added to headers object
     - `apiPut` — Added to headers object
     - `apiPostFile` — Added to headers object (FormData upload)
     - `apiGetBlob` — Added to headers object

1. **Hooks** — No changes required. All hooks (`useDocument`, `useSuggestions`, `useAnalysis`) call service functions which now automatically include the header.

**Pattern:**

- Single source of truth for userId: `currentUserId` module variable in `api.ts`
- Header sent on every request — backend can extract and use for partitioning
- Default `"_anonymous"` ensures backward compatibility with existing data
- `setUserId` / `getUserId` exported for future authentication integration (e.g., MSAL)

**Verification:** `npx pnpm run build` succeeded with zero TypeScript errors. Bundle size unchanged (header is just a string).

**Key Files:**

- `marginalia-app/src/services/api.ts` — userId management + header injection
- `marginalia-app/src/types/document.ts`, `session.ts`, `suggestion.ts` — Type additions

**Team Coordination:** Parallel backend work by Gilfoyle. Frontend and backend userId implementations fully aligned.
