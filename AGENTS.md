# Marginalia — Agent Guidelines

Full implementation patterns: [.github/copilot-instructions.md](.github/copilot-instructions.md).

## Layout

| Path | Purpose |
|---|---|
| `marginalia-service/` | .NET 10 backend — C#, ASP.NET Core, .NET Aspire |
| `marginalia-app/` | React 19 frontend — TypeScript, Vite |
| `infra/` | Azure Bicep IaC |
| `tests/smoke/` | PowerShell smoke tests |

## Commands

### Backend (`marginalia-service/`)

```bash
dotnet format Marginalia.slnx --verify-no-changes    # Lint
dotnet build Marginalia.slnx                         # Build
dotnet test --solution Marginalia.slnx --filter TestCategory=Unit --no-build  # Test
```

### Frontend (`marginalia-app/`)

```bash
pnpm install     # if package.json changed
pnpm lint
pnpm run build   # includes TypeScript check
pnpm test
```

### Markdown and Infrastructure (repo root)

```bash
pnpm lint:md
az bicep lint --file infra/main.bicep
```

## CI Checks — Must Pass

| Job | Fails on |
|---|---|
| `dotnet-lint` | `dotnet format --verify-no-changes` errors |
| `dotnet-build-test` | Build errors or unit test failures |
| `build-and-publish-frontend-app` | ESLint, Vitest, or TypeScript errors |
| Bicep lint | Bicep syntax errors |

**Production deploys on `v*` tag pushes only, after E2E tests pass.**

## Change Checklist

After every code change:

1. Run build + lint + test for each affected subsystem
1. Add or update unit tests for all changed logic
1. Run `pnpm lint:md` if any `.md` files changed
1. **Never** add package versions to `.csproj` files — all versions in `Directory.Packages.props`
1. **Never** use the null-forgiving operator (`!`) without a comment explaining why

## Conventions

| Concern | Rule |
|---|---|
| Warnings | Treated as errors in .NET and TypeScript — never suppress without justification |
| Async | All I/O async; pass `CancellationToken` in .NET; `async`/`await` in TypeScript |
| Dead code | Remove unused imports, variables, and functions — never leave commented-out code |
| Error responses | `{ error: "message" }` from API; throw `ApiError` in frontend |
| Commit messages | Conventional: `feat:`, `fix:`, `docs:`, `chore:`, `refactor:`, `test:` |

## Permission Boundaries

**Freely:** edit source, run build/test/lint commands, add or modify tests.
**Ask first:** install new packages, delete files, push to shared branches, modify infrastructure.
