# Marginalia — Agent Guidelines

This document provides broad coding standards and conventions for all agents working on the Marginalia repository.
For project-specific implementation details, see [.github/copilot-instructions.md](.github/copilot-instructions.md).

## Repository Overview

Marginalia is a full-stack document analysis and annotation web application organized as a monorepo:

- `marginalia-service/` — .NET 10 backend (C#, ASP.NET Core, .NET Aspire)
- `marginalia-app/` — React 19 frontend (TypeScript, Vite)
- `infra/` — Infrastructure as Code (Azure Bicep)
- Root — Monorepo orchestration and markdown linting

## Mandatory Validation

After applying any code changes, agents **must** run the relevant build, test, and lint commands to validate correctness before completing their work.

### Backend Service (`marginalia-service/`)

```bash
cd marginalia-service
dotnet format Marginalia.slnx --verify-no-changes    # Lint
dotnet build Marginalia.slnx                         # Build
dotnet test --solution Marginalia.slnx --no-build    # Test
```

### Frontend App (`marginalia-app/`)

```bash
cd marginalia-app
pnpm install          # Install dependencies (if changed)
pnpm lint             # Lint
pnpm run build        # Build (includes TypeScript check)
pnpm test             # Test
```

### Markdown (root)

```bash
pnpm lint:md          # Lint all markdown files
```

### Infrastructure (`infra/`)

```bash
az bicep lint --file infra/main.bicep
```

## General Code Standards

1. **Warnings as errors** — both .NET (`TreatWarningsAsErrors`) and TypeScript (`strict` mode) treat warnings as errors. Do not suppress or ignore warnings without justification.
1. **No dead code** — do not leave unused imports, variables, functions, or commented-out code.
1. **Immutability by default** — prefer `sealed record` in C# and `const`/`readonly` patterns in TypeScript.
1. **Async all the way** — all I/O operations must be async. Always pass `CancellationToken` in .NET. Always use `async`/`await` in TypeScript.
1. **Nullable references enabled** — C# projects have `<Nullable>enable</Nullable>`. Handle nullability explicitly; do not use the null-forgiving operator (`!`) without justification.
1. **Centralized package management** — .NET uses `Directory.Packages.props` for package versions. Never specify versions in individual `.csproj` files.
1. **Structured error responses** — return `{ error: "message" }` from API controllers and throw `ApiError` in the frontend.

## Naming Conventions

### General

| Element        | Convention                         | Example                                |
| -------------- | ---------------------------------- | -------------------------------------- |
| Git branch     | `kebab-case`                       | `fix-upload-validation`                |
| Markdown files | `UPPER-CASE.md` or `kebab-case.md` | `README.md`, `copilot-instructions.md` |

### .NET (C#)

| Element        | Convention                           | Example                           |
| -------------- | ------------------------------------ | --------------------------------- |
| Namespace      | PascalCase hierarchy                 | `Marginalia.Domain.Models`        |
| Class / Record | PascalCase, sealed                   | `DocumentsController`, `Document` |
| Interface      | `I` prefix + PascalCase              | `IDocumentRepository`             |
| Method         | PascalCase                           | `GetByIdAsync`                    |
| Async method   | `Async` suffix                       | `SaveAsync`, `DeleteAsync`        |
| Property       | PascalCase                           | `UserId`, `Filename`              |
| Private field  | `_camelCase`                         | `_documentRepository`, `_logger`  |
| Local variable | camelCase                            | `documentId`, `userSession`       |
| Constant       | PascalCase                           | `MaxFileSize`                     |
| File name      | Matches type name                    | `DocumentsController.cs`          |
| Test class     | `{ClassUnderTest}Tests`              | `DocumentTests`                   |
| JSON property  | camelCase via `[JsonPropertyName]`   | `"userId"`, `"fileName"`          |

### TypeScript / React

| Element             | Convention                            | Example                            |
| ------------------- | ------------------------------------- | ---------------------------------- |
| Component           | PascalCase `.tsx`                     | `SuggestionCard.tsx`               |
| Hook                | `use` prefix, camelCase `.ts`         | `useDocuments.ts`                  |
| Service             | camelCase `.ts`                       | `documentService.ts`               |
| Type / Interface    | PascalCase                            | `Document`, `ApiError`             |
| Type file           | camelCase `.ts`                       | `document.ts`, `suggestion.ts`     |
| Function / variable | camelCase                             | `loadDocuments`, `isLoading`       |
| Constant            | UPPER_SNAKE_CASE                      | `TONE_OPTIONS`, `CHUNK_SIZE_CHARS` |
| Test file           | `{name}.test.tsx` or `{name}.test.ts` | `SuggestionCard.test.tsx`          |
| CSS class           | Tailwind utility classes              | —                                  |

### Bicep (Infrastructure)

| Element         | Convention                          | Example                            |
| --------------- | ----------------------------------- | ---------------------------------- |
| File name       | kebab-case `.bicep`                 | `main.bicep`, `role_foundry.bicep` |
| Parameter       | camelCase                           | `environmentName`, `location`      |
| Variable        | camelCase                           | `resourceToken`, `tags`            |
| Resource naming | `${abbreviation}${environmentName}` | `log-${environmentName}`           |

## File Organization

### .NET Project Structure

```text
marginalia-service/
├── src/
│   ├── Api/                    # ASP.NET Core controllers, DI config
│   │   └── Controllers/        # [ApiController] classes
│   ├── Domain/                 # Domain models, interfaces, configuration
│   │   ├── Configuration/      # Options pattern classes
│   │   ├── Interfaces/         # Repository & service contracts
│   │   └── Models/             # Sealed record types
│   ├── Infrastructure/         # External service implementations
│   │   ├── Repositories/       # Cosmos DB + in-memory repos
│   │   └── Services/           # AI service integrations
│   └── Orchestration/          # .NET Aspire AppHost & ServiceDefaults
├── tests/
│   ├── unit/                   # MSTest v4 unit tests
│   └── integration/            # Integration & orchestration tests
```

### React App Structure

```text
marginalia-app/src/
├── components/                 # UI components
│   └── ui/                     # shadcn/ui base components
├── hooks/                      # Custom React hooks
├── pages/                      # Route-level page components
├── services/                   # API client layer
├── types/                      # TypeScript type definitions
├── App.tsx                     # Router root
├── main.tsx                    # Entry point
└── index.css                   # Global Tailwind styles
```

Test files mirror the `src/` structure under a `tests/` directory at the project root.

## Architecture & Design Patterns

1. **Layered architecture** — the .NET service follows Api → Domain → Infrastructure layering. Domain must not reference Infrastructure or Api.
1. **Repository pattern** — data access through interfaces (`IDocumentRepository`, `ISessionRepository`) with Cosmos DB and in-memory implementations.
1. **Options pattern** — configuration via strongly-typed options classes (e.g., `LlmEndpointOptions`) registered in DI.
1. **Sealed types** — all controllers, models, records, and service implementations should be `sealed`.
1. **Custom hooks** — React state and side effects are encapsulated in custom hooks (`useDocuments`, `useAnalysis`, `useSuggestions`). Hooks return an object with state properties and action methods.
1. **Service layer** — the React app uses a centralized API client (`api.ts`) with typed service modules (`documentService.ts`, `suggestionService.ts`). API calls flow through `apiGet`, `apiPost`, `apiPut`, `apiPostFile`, `apiGetBlob` helpers.
1. **Multi-tenancy** — all data is partitioned by `userId`. The API extracts user ID from the `X-User-Id` header.

## Testing Standards

### .NET

- **Framework**: MSTest v4 with `[TestClass]`, `[TestMethod]`, `[TestCategory("Unit")]`
- **Assertions**: FluentAssertions (`.Should().Be()`, `.Should().HaveCount()`)
- **Mocking**: NSubstitute
- **Parallelism**: `[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]`
- **Setup/teardown**: `[TestInitialize]` / `[TestCleanup]`
- Test classes are named `{ClassUnderTest}Tests` and placed in a folder matching the source structure.

### React

- **Framework**: Vitest with `describe` / `it` blocks
- **Rendering**: React Testing Library (`render`, `screen`, `userEvent`)
- **Accessibility**: jest-axe for a11y assertions
- **Mocking**: `vi.fn()` for functions, global `fetch` mock for API tests
- **Hooks**: `renderHook()` from React Testing Library
- Tests are grouped by feature with `describe` blocks and use `beforeEach`/`afterEach` for setup.

## Commit & PR Standards

1. Use conventional commit messages: `feat:`, `fix:`, `docs:`, `chore:`, `refactor:`, `test:`.
1. Reference issues in PR descriptions: `Closes #<issue-number>`.
1. Each PR should pass all CI checks (lint, build, test) before merge.
