# Project Context

- **Owner:** Mary MacGregor-Reid
- **Project:** Marginalia — AI-powered narrative flow editor for long-form non-fiction writers. Helps authors refine, expand, and ensure stylistic consistency in manuscripts with AI-generated suggestions and fine-grained editorial control.
- **Stack:** React (frontend), .NET 10 / ASP.NET (backend), Azure Functions, Azure AI / Microsoft Foundry, Google Docs API integration
- **Created:** 2026-03-22

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

- **NSubstitute null argument matching:** When using `null` alongside `Arg.Any<>` matchers in NSubstitute, use `Arg.Any<string?>()` for all args of the same type to avoid `AmbiguousArgumentsException`. Never mix literal `null` with `Arg.Any<>` matchers.

- **Domain models are records with no validation:** TextRange, Document, Suggestion are simple records. They accept invalid values (e.g., Start > End, negative ranges). Validation must live in the service/API layer — domain models are just data carriers.

- **Axe nested-interactive pattern in shadcn/ui:** SuggestionCard and DocumentUploader both have nested interactive elements (buttons inside focusable containers). This is a common pattern with card-based UIs that act as both clickable regions and containers for action buttons. Flag for the frontend team to resolve — consider moving action buttons outside the focusable header or using aria-owns.

- **Test infrastructure:** Backend uses MSTest + NSubstitute + FluentAssertions (via MSTest.Sdk in global.json). Frontend uses Vitest + @testing-library/react + jest-axe. Both test setups parallelize by default. `jest-axe/extend-expect` must be imported in `vitest.setup.ts` for `toHaveNoViolations()` matcher.

- **Test double strategy:** Created a `TestDocumentRepository` using ConcurrentDictionary in the test project to verify the IDocumentRepository contract. This is preferable to mocking the interface (which only tests the mock). When InMemoryDocumentRepository lands, these contract tests validate the real implementation.

- **Frontend service architecture:** The API layer uses a base client pattern (`api.ts` with `apiGet`, `apiPost`, etc.) with domain-specific service modules (`documentService.ts`, `configService.ts`, `suggestionService.ts`). Tests should mock `fetch` globally, not individual service functions, to verify the full request chain.

- **SuggestionStatus serializes as string:** The `[JsonConverter(typeof(JsonStringEnumConverter))]` attribute ensures `"Pending"` not `0` in JSON. Frontend types match with string union type `'Pending' | 'Accepted' | 'Rejected' | 'Modified'`. Tests verify this round-trips correctly.

- **Aspire orchestration testing pattern:** Use `DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>()` to build the Aspire app model in tests. Verify resources exist via `DistributedApplicationModel.Resources` from DI — don't make HTTP calls to services (that requires live credentials). Tests validate the wiring, not the running services. Uses `MSTest.Sdk/4.1.0` (not `Microsoft.NET.Sdk`), `Aspire.Hosting.Testing`, and FluentAssertions.

- **Aspire test project conventions:** Orchestration integration tests live in `tests/integration/Orchestration.IntegrationTests/` with namespace `Marginalia.Orchestration.IntegrationTests`. ProjectReference to AppHost enables the `Projects.Marginalia_AppHost` generated type. Central package management pins `Aspire.Hosting.Testing` and `coverlet.collector` versions in `Directory.Packages.props`.

### Orchestration Integration Tests (2026-03-22)

**Architecture:**

- Separate project: `tests/integration/Orchestration.IntegrationTests/`
- SDK: `MSTest.Sdk/4.1.0` (not Microsoft.NET.Sdk) matching prompt-babbler pattern
- Uses `DistributedApplicationTestingBuilder.CreateAsync<Projects.Marginalia_AppHost>()` to build app model
- Verifies resources via `DistributedApplicationModel.Resources` from DI
- Does NOT make HTTP calls (avoids Azure credential requirement)

**Strategy:**

- Tests verify orchestration model wiring only (resource existence, naming, relationships)
- Scope is limited and specific per design
- Can run in CI without Azure credentials
- Separate concern from API integration tests (which use `Microsoft.AspNetCore.Mvc.Testing`)

**Package Additions:**

- Aspire.Hosting.Testing: 9.2.0
- coverlet.collector: 6.0.4

### Home Page Feature Tests (2026-03-29)

**Backend tests (all compile and pass — 42 tests):**

- `tests/unit/Domain/DocumentStatusTests.cs` — DocumentStatus enum serialization (Draft, Analyzed), JSON string representation, all enum values
- `tests/unit/Domain/DocumentSummaryTests.cs` — DocumentSummary DTO construction, serialization (camelCase), deserialization, verifies no `content` or `suggestions` fields (lightweight projection)
- `tests/unit/Domain/DocumentListResponseTests.cs` — DocumentListResponse wrapper, wraps in `documents` property (not bare array), empty/multiple document cases
- `tests/unit/Domain/DocumentHomePageFieldsTests.cs` — Document model with Title, Status, CreatedAt, UpdatedAt fields, status transitions (Draft → Analyzed), timestamp preservation, title generation defaults, suggestion count projection
- `tests/unit/Domain/PasteDocumentRequestTitleTests.cs` — Optional Title field on PasteDocumentRequest, serialization/deserialization with and without title

**Key finding:** Gilfoyle already added Title, Status, CreatedAt, UpdatedAt to the Document model and Title to PasteDocumentRequest (with sensible defaults: Title = "", Status = Draft, timestamps = MinValue). All backend tests passed immediately.

**Frontend tests:**

- `tests/components/DocumentUploader.title.test.tsx` — Title input field tests (4 fail: title input not rendered yet, 1 pass: upload-without-title backward compat). Expected TDD red — Dinesh needs to add the title input.
- `tests/pages/HomePage.test.tsx` — Full Home page contract: document list, empty state, loading spinner, error state, navigation, "New Manuscript" button. Won't compile until `src/pages/HomePage.tsx` is created.
- `tests/hooks/useDocuments.test.ts` — useDocuments hook: fetch on mount, loading state, error handling, empty list, correct API endpoint. Won't compile until `src/hooks/useDocuments.ts` is created.

**Testing decisions:**

- Mock at the `fetch` boundary for frontend tests (not at service layer) — tests the full request chain
- Backend DTO tests verify serialization excludes heavyweight fields (content, suggestions) — enforces the lightweight listing contract
- Title generation format tests use string interpolation to verify the expected format without depending on controller logic

### 2026-03-29 — Cross-Agent: Home Page Feature Complete

**Richard (Lead):** API design provided the spec for all test contracts. Flat REST hierarchy, DocumentSummary DTO without content/suggestions, title defaults.

**Gilfoyle (Backend):** All backend endpoints implemented. 42 backend tests pass against his implementation. Legacy Cosmos documents handled cleanly — no migration needed.

**Dinesh (Frontend):** React Router, HomePage, useDocuments hook, title input all implemented. Frontend tests pass against his implementation. Build clean, 82+ tests pass.

**Session Completion (2026-03-22 Session Log):**

- Orchestration.IntegrationTests project created with resource verification tests
- Integration tests passing alongside 79 backend tests
- Aspire test strategy decision documented and merged into team decisions.md
- Integration log created: `.squad/orchestration-log/2026-03-22T01_25_00Z-jared.md`

### UserId Multi-Tenancy Tests (2026-03-22)

**Test Coverage for Cosmos DB userId Support:**

Created comprehensive test suite for the userId partitioning changes being implemented by Gilfoyle. Tests validate the NEW repository interface contracts (with userId parameters) and userId defaulting behavior. Total: **43 tests** covering repository contracts, domain defaulting, and controller header extraction.

**Files Created:**

1. **`tests/unit/Repositories/UserIdDocumentRepositoryContractTests.cs`** — 14 tests validating IDocumentRepository contract with userId:
   - GetByIdAsync with userId + id composite key
   - GetByUserAsync returns only user's documents
   - SaveAsync stores userId correctly
   - DeleteAsync removes by userId + id
   - User isolation (different users can't see each other's documents)
   - Concurrent operations with multiple users
   - Edge cases (not found returns null, duplicates handled)

1. **`tests/unit/Repositories/UserIdSessionRepositoryContractTests.cs`** — 12 tests validating ISessionRepository contract with userId:
   - GetByIdAsync with userId + sessionId composite key
   - SaveAsync stores userId correctly
   - AddDocumentToSessionAsync with userId parameter
   - User isolation (same sessionId for different users creates separate sessions)
   - Concurrent operations

1. **`tests/unit/Domain/UserIdDefaultingTests.cs`** — 9 tests validating "_anonymous" default behavior:
   - Document, UserSession, and Suggestion all default to "_anonymous" when userId not specified
   - Explicit userId values are preserved
   - Record `with` syntax preserves userId

1. **`tests/integration/Controllers/UserIdHeaderExtractionTests.cs`** — 8 tests validating controller X-User-Id header extraction:
   - Controllers extract userId from X-User-Id header
   - Missing header defaults to "_anonymous"
   - Empty/whitespace header defaults to "_anonymous"
   - User isolation in GET operations (user-bob can't access user-alice's documents)

**Test Double Strategy:**

- Created test doubles (`TestUserIdDocumentRepository`, `TestUserIdSessionRepository`) implementing the NEW interface signatures
- Test doubles use composite keys (`userId:id`) to simulate multi-tenant partitioning
- Tests validate interface contracts, not implementation details
- When Gilfoyle's Cosmos repositories land, these same tests will validate the real implementations

**Build Status:**

Tests do NOT compile yet — expected, as Gilfoyle is implementing source changes in parallel:

- Domain models need `UserId` property added (Document, UserSession, Suggestion)
- Repository interfaces need userId parameters added (IDocumentRepository, ISessionRepository)
- Controllers need X-User-Id header extraction logic
- Infrastructure project has Cosmos SDK dependency issue (missing Newtonsoft.Json package reference)

**Test Framework Consistency:**

- Unit tests: MSTest + NSubstitute + FluentAssertions (existing pattern)
- Integration tests: MSTest + Microsoft.AspNetCore.Mvc.Testing + FluentAssertions (existing pattern)
- All tests follow existing naming conventions and structure

**Coverage:**

- ✅ Repository contract tests with userId partitioning (26 unit tests)
- ✅ UserId defaulting tests (9 tests)
- ✅ Controller header extraction tests (8 integration tests)
- ✅ Multi-tenant isolation tests
- ✅ Edge cases (null, empty, whitespace)
- ✅ Concurrent operation safety
