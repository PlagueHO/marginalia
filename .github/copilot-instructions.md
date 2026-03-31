# Copilot Instructions — Marginalia

This file provides project-specific implementation guidance for the Marginalia repository.
For broad coding standards, naming conventions, and file organization, see [AGENTS.md](../../AGENTS.md).

## .NET Backend Implementation Details

### Controller Patterns

- Controllers use attribute routing with `[ApiController]` and `[Route("api/[controller]")]`.
- All controllers are `sealed` classes.
- Inject dependencies via constructor parameters (`ILogger<T>`, service interfaces).
- Extract user identity from the `X-User-Id` header, defaulting to `"_anonymous"` when absent.
- Return `ActionResult<T>` from action methods. Use `Ok()`, `Created()`, `BadRequest()`, `NotFound()` — never throw exceptions for expected conditions.
- Validate file uploads for type (`.docx` only) and size (50 MB max) before processing.
- Log all operations at `Information` level for auditing; use `Warning` for recoverable errors.

### Domain Model Patterns

- All models are `sealed record` types with init-only properties.
- Use `[JsonPropertyName("camelCase")]` attributes for JSON serialization.
- Default collections to empty (e.g., `List<Suggestion> Suggestions { get; init; } = []`).
- Enums: `DocumentSource` (Local, GoogleDocs), `DocumentStatus` (Draft, Analyzed), `SuggestionStatus` (Pending, Accepted, Rejected, Modified).
- All service and repository contracts are defined as interfaces in `Marginalia.Domain.Interfaces`.

### Repository Implementation Patterns

- Cosmos DB repositories use `/userId` as the partition key.
- Queries use parameterized `QueryDefinition` with `@parameter` syntax — never string interpolation.
- Handle `CosmosException` by checking `HttpStatusCode` (return `null` for `NotFound`).
- All methods accept `CancellationToken` and are fully async (`Task<T>`).
- Use `UpsertItemAsync` for idempotent saves.
- Provide matching in-memory implementations for testing (e.g., `InMemoryDocumentRepository`).

### AI / LLM Service Patterns

- Use `IChatClient` from `Microsoft.Extensions.AI` for LLM interactions.
- Configure endpoints via the options pattern (`LlmEndpointOptions`) with environment variable overrides (`FOUNDRY_ENDPOINT`, `FOUNDRY_MODEL_NAME`).
- Chunk text at approximately 6000 characters (~3 pages) for analysis.
- Construct separate system and user prompts for editorial analysis.
- Parse JSON responses from the LLM into domain models with proper error handling.

### Dependency Injection

- Register services in `Program.cs` using the standard pattern.
- Use `AddSingleton`, `AddScoped`, or `AddTransient` as appropriate for service lifetimes.
- Configuration objects are registered via `.Configure<T>(configuration.GetSection("..."))`.
- API configuration includes CORS with allowed origins from `CORS:AllowedOrigins`, camelCase JSON, and 50 MB request body limit.

### .NET Aspire Orchestration

- The `AppHost` registers all services with resource references and `WaitFor()` dependencies.
- Cosmos DB containers use `/userId` partition key: `documents`, `sessions`, `suggestions`.
- The frontend is registered as a Vite app with `pnpm` package manager and external HTTP endpoints.
- Service defaults configure OpenTelemetry (logging, metrics, tracing), health checks (`/health`, `/alive`), service discovery, and HTTP resilience handlers.

## React Frontend Implementation Details

### Component Patterns

- All components are functional components using hooks.
- Props are defined with TypeScript `interface` declarations.
- Use `useCallback` with explicit dependency arrays for memoized handlers.
- Use `useEffect` with cleanup functions for side effects.
- Icons are exclusively from the `lucide-react` package.
- UI primitives come from shadcn/ui (Radix UI + Tailwind CSS v4) in `components/ui/`.
- Do not add new UI libraries — use existing shadcn/ui components or generate new ones via the shadcn CLI.

### Custom Hook Patterns

Hooks follow a consistent structure:

```typescript
export function useExample() {
  const [data, setData] = useState<Type | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await exampleService.getData();
      setData(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : "An error occurred");
    } finally {
      setIsLoading(false);
    }
  }, []);

  return { data, isLoading, error, loadData };
}
```

Return an object containing state properties and action methods — never return arrays.

### API Service Layer Patterns

- All API requests flow through the centralized client in `services/api.ts`.
- Use the typed helpers: `apiGet<T>()`, `apiPost<T>()`, `apiPut<T>()`, `apiPostFile<T>()`, `apiGetBlob()`.
- The `X-User-Id` header is injected automatically by the API client.
- Throw `ApiError` (with `message` and `statusCode`) for non-OK responses.
- Service modules (`documentService.ts`, `suggestionService.ts`, `configService.ts`) are thin wrappers that call the API client and manage endpoints.
- Barrel-export all services from `services/index.ts` and all types from `types/index.ts`.

### Type Definition Patterns

- Define types in `types/` with one file per domain concept (`document.ts`, `suggestion.ts`, `session.ts`, `api.ts`).
- Use `type` for unions and simple aliases, `interface` for object shapes.
- Keep types synchronized with the backend domain models — property names and shapes must match the API JSON contract.
- Export all types from `types/index.ts`.

### Path Aliases and Imports

- Use the `@/` path alias for all internal imports (e.g., `import { Document } from "@/types"`).
- Never use relative paths that traverse above the current directory (`../..`).

### Styling

- Use Tailwind CSS v4 utility classes exclusively — no custom CSS files beyond `index.css`.
- shadcn/ui component variants use `class-variance-authority` (CVA).
- Use `clsx` or `cn()` (from `lib/utils.ts`) for conditional class merging.

## Infrastructure (Bicep) Implementation Details

- Use Azure Verified Modules (AVM) from `br/public:avm/` where available.
- Load shared configuration from JSON files using `loadJsonContent()`.
- Generate unique resource names with `uniqueString(subscription().id, environmentName, location)`.
- Tag all resources with at minimum `azd-env-name` and `project` tags.
- Resource naming follows `abbreviations.json` patterns: `${abbrs.resourceType}${environmentName}`.
- Deploy at subscription scope with resource group creation in the template.
- Support public network access toggle via the `enablePublicNetworkAccess` parameter.

## OpenTelemetry

- The backend uses OpenTelemetry for logging, metrics (AspNetCore, HTTP, Runtime), and tracing configured in ServiceDefaults.
- The frontend initializes OpenTelemetry in `telemetry.ts` with Aspire-injected environment variables (`__OTEL_*__`).
- When adding new services or significant operations, include appropriate telemetry instrumentation.
