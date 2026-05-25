# Copilot Instructions — Marginalia

Marginalia is a .NET 10 + React 19 full-stack document analysis application backed by Azure AI Foundry.
See [AGENTS.md](../AGENTS.md) for layout, commands, and CI pipeline.

## Security Rules

- **Cosmos DB**: always use parameterized `QueryDefinition` with `@param` syntax — never string interpolation.
- **Prompt construction**: treat all user-supplied text (guidance, document content) as untrusted; never embed it unescaped in system prompts.
- **User identity**: extract from `X-User-Id` header; default to `"_anonymous"` — never accept identity from the request body.
- **File uploads**: validate `.docx` type and 50 MB size limit before processing.
- **Secrets**: never log or return connection strings, API keys, or access codes.
- **Logging safety**: never log raw user-derived text, titles, file names, or file paths; log metadata only (counts, lengths, durations, booleans, IDs).

## .NET Backend Patterns

### Controllers

- `[ApiController]` + `[Route("api/[controller]")]`; all controllers are `sealed`.
- Inject `ILogger<T>` and service interfaces via constructor.
- Return `ActionResult<T>`; use `Ok()`, `Created()`, `BadRequest()`, `NotFound()` — never throw for expected conditions.
- Log all operations at `Information` level; recoverable errors at `Warning`.

### Domain Models

- `sealed record` with init-only properties and `[JsonPropertyName("camelCase")]` attributes.
- Default collections: `List<T> Items { get; init; } = []`.
- Enums: `DocumentSource` (Local, GoogleDocs), `DocumentStatus` (Draft, Analyzed), `SuggestionStatus` (Pending, Accepted, Rejected, Modified).
- All service and repository contracts are interfaces in `Marginalia.Domain.Interfaces`.

### Repositories

- `/userId` partition key for all Cosmos DB containers.
- `UpsertItemAsync` for idempotent saves; catch `CosmosException`, check `HttpStatusCode`, return `null` for `NotFound`.
- Provide matching in-memory implementations (e.g., `InMemoryDocumentRepository`) for unit tests.
- All methods accept `CancellationToken` and return `Task<T>`.

### AI / LLM Service

- Use `IChatClient` from `Microsoft.Extensions.AI`; configure via `LlmEndpointOptions` (env: `FOUNDRY_ENDPOINT`, `FOUNDRY_MODEL_NAME`).
- Chunk text at ~6000 characters; construct separate system and user prompts; parse JSON responses with error handling.

### Configuration and DI

- Options pattern via `.Configure<T>(configuration.GetSection(...))`.
- CORS allowed origins from `CORS:AllowedOrigins`; JSON serialization camelCase; request body limit 50 MB.
- `AppHost` registers all services with `WaitFor()` dependencies; frontend is a Vite app with `pnpm`.
- OpenTelemetry logging in ServiceDefaults must keep the centralized sanitizing processor enabled.

## React Frontend Patterns

### Components

- Functional components with TypeScript `interface` props; `useCallback` with explicit dependency arrays.
- Icons: `lucide-react` only. UI primitives: shadcn/ui in `components/ui/`. Never add new UI libraries.

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

Return an object from hooks — never an array.

### API Service Layer

- All requests via typed helpers in `services/api.ts`: `apiGet<T>()`, `apiPost<T>()`, `apiPut<T>()`, `apiPostFile<T>()`, `apiGetBlob()`.
- `X-User-Id` header injected automatically. Throw `ApiError` (with `message` and `statusCode`) for non-OK responses.
- Service modules are thin wrappers; barrel-export from `services/index.ts` and `types/index.ts`.
- Use `@/` path alias for all internal imports — never traverse above `src/` with `../..`.
- Tailwind CSS v4 utility classes only; use `cn()` from `lib/utils.ts` for conditional merging.

## Naming Conventions

| Element | Convention | Example |
|---|---|---|
| C# class / record | `sealed` PascalCase | `DocumentsController` |
| C# interface | `I` prefix + PascalCase | `IDocumentRepository` |
| C# async method | PascalCase + `Async` suffix | `GetByIdAsync` |
| C# private field | `_camelCase` | `_documentRepository` |
| C# JSON property | `[JsonPropertyName("camelCase")]` | `"userId"`, `"fileName"` |
| React component | PascalCase `.tsx` | `SuggestionCard.tsx` |
| React hook | `use` prefix, camelCase `.ts` | `useDocuments.ts` |
| TypeScript constant | `UPPER_SNAKE_CASE` | `TONE_OPTIONS` |
| TypeScript test file | `{name}.test.{ts,tsx}` | `SuggestionCard.test.tsx` |
| Bicep file | `kebab-case` or `snake_case` | `role_foundry.bicep` |
| Git branch | `kebab-case` | `fix-upload-validation` |

## Testing

### .NET (MSTest v4 + MTP)

- `[TestClass]`, `[TestMethod]`, `[TestCategory("Unit")]`; runner configured in `global.json`.
- Assertions: FluentAssertions. Mocking: NSubstitute. Parallelism: `[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]`.
- Test class naming: `{ClassUnderTest}Tests`, folder mirrors source structure.

### React (Vitest)

- `describe` / `it` blocks; React Testing Library (`render`, `screen`, `userEvent`); jest-axe for a11y.
- `vi.fn()` for function mocks; `renderHook()` for custom hook tests.

## Infrastructure and Telemetry

- Bicep: use AVM modules (`br/public:avm/`); names via `uniqueString()`; tag with `azd-env-name` + `project`.
- Deploy at subscription scope; support `enablePublicNetworkAccess` toggle.
- Backend OTel configured in `ServiceDefaults` (logging, metrics, tracing).
- Frontend OTel initialized in `telemetry.ts` using Aspire-injected `__OTEL_*__` env vars.

