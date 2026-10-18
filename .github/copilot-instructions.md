# Copilot instructions — Marginalia

See [AGENTS.md](../AGENTS.md) for layout and commands.

Marginalia analyzes Word manuscripts and suggests editorial changes. It uses
.NET 10, React 19, TypeScript, .NET Aspire, Cosmos DB, and Azure AI Foundry.

## Security

- Build Cosmos queries with parameterized `QueryDefinition` and `@param` values; never interpolate input.
- Keep user text out of system prompts; pass it as user content.
- Read identity from `X-User-Id`; default to `"_anonymous"`. Never accept identity from request bodies.
- Validate `.docx` uploads and enforce the 50 MB limit before processing.
- Never log or return secrets, connection strings, API keys, or access codes.
- Never log raw user text, titles, file names, or paths. Log metadata only.

## .NET patterns

- Seal API controllers; use `[ApiController]` and `[Route("api/[controller]")]`.
- Inject loggers and services through constructors.
- Return `ActionResult<T>` and use `Ok`, `Created`, `BadRequest`, or `NotFound` for expected errors.
- Use sealed records, init-only properties, and `[JsonPropertyName("camelCase")]` for domain models.
- Default collection properties to `[]`; preserve `DocumentSource` (Local, GoogleDocs), `DocumentStatus` (Draft, Analyzed), and `SuggestionStatus` (Pending, Accepted, Rejected, Modified).
- Define service and repository interfaces in `Marginalia.Domain.Interfaces`; provide matching in-memory repositories for tests.
- Use `/userId` as the Cosmos partition key. Use `UpsertItemAsync` for idempotent saves.
- Catch `CosmosException`; return `null` for `NotFound`.
- Accept `CancellationToken` on I/O methods and return `Task<T>`.
- Use `IChatClient` and `LlmEndpointOptions`; read endpoint and model settings from `FOUNDRY_ENDPOINT` and `FOUNDRY_MODEL_NAME`.
- Chunk analysis text at about 6,000 characters and handle invalid JSON responses.
- Configure options from their sections with `.Configure<T>(configuration.GetSection(...))`.
- Read CORS origins from `CORS:AllowedOrigins`; use camelCase JSON and a 50 MB request limit.
- Register Aspire dependencies with `WaitFor()`.
- Keep the ServiceDefaults log-sanitizing processor enabled.
- Keep backend OpenTelemetry logging, metrics, and tracing in ServiceDefaults.
- Put package versions in `Directory.Packages.props`, not `.csproj` files.
- Explain any use of the C# null-forgiving operator (`!`) with a comment.

## React patterns

- Use functional components and TypeScript `interface` props.
- Give `useCallback` explicit dependency arrays.
- Return objects from hooks, not arrays.
- Use `apiGet`, `apiPost`, `apiPut`, `apiDelete`, `apiPostFile`, and `apiGetBlob` from `services/api.ts`; throw `ApiError` for failed responses.
- Keep service modules thin; export them from `services/index.ts` and types from `types/index.ts`.
- Use the `@/` alias for internal imports; do not traverse above `src/`.
- Use Tailwind CSS v4 classes and `cn()` for conditional class names.
- Use `lucide-react` icons and shadcn/ui primitives; do not add UI libraries.

## Naming

| Element | Convention | Example |
|---|---|---|
| C# types | PascalCase; interfaces start with `I` | `IDocumentRepository` |
| C# private fields | `_camelCase` | `_documentRepository` |
| C# async methods | PascalCase with `Async` suffix | `GetByIdAsync` |
| React components | PascalCase `.tsx` | `SuggestionCard.tsx` |
| React hooks | `use` + camelCase `.ts` | `useDocuments.ts` |
| TypeScript constants | `UPPER_SNAKE_CASE` | `TONE_OPTIONS` |
| TypeScript tests | `{name}.test.{ts,tsx}` | `SuggestionCard.test.tsx` |
| Bicep files | kebab-case or snake_case | `role_foundry.bicep` |

## Testing and infrastructure

- Write .NET tests with MSTest v4; use FluentAssertions and NSubstitute.
- Mark unit tests with `[TestCategory("Unit")]`; name test classes `{ClassUnderTest}Tests`.
- Write frontend tests with Vitest and React Testing Library; use `vi.fn()` for mocks and jest-axe for accessibility tests.
- Use AVM modules for Bicep resources; deploy at subscription scope, generate names with `uniqueString()`, and support `enablePublicNetworkAccess`.
- Tag Azure resources with `azd-env-name` and `project`.
- Keep frontend OpenTelemetry configuration in `telemetry.ts` and use Aspire-injected `__OTEL_*__` variables.
