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

Run backend commands from `marginalia-service/`.

```bash
dotnet format Marginalia.slnx --verify-no-changes --severity error
dotnet build Marginalia.slnx
dotnet test --solution Marginalia.slnx --filter TestCategory=Unit --ignore-exit-code 8 --no-build
```

Run frontend commands from `marginalia-app/`.

```bash
pnpm install
pnpm lint
pnpm run build
pnpm test
```

Run Markdown and infrastructure commands from the repository root.

```bash
pnpm lint:md
az bicep lint --file infra/main.bicep
```

## CI checks

- **Markdown lint**: Fails on Markdown lint errors.
- **Infrastructure lint**: Fails on Bicep lint errors.
- **Backend lint**: Fails on formatting or logging-safety errors.
- **Backend build and tests**: Fails on build or unit-test errors.
- **Frontend build**: Fails on ESLint, Vitest, or TypeScript errors.
- **Docs build**: Fails when the documentation site does not build.

**Production deploys on `v*` tag pushes only, after E2E tests pass.**

## Change Checklist

For each change:

1. Run lint, build, and tests for each affected code subsystem.
1. Add or update unit tests for changed logic.
1. Run the Markdown lint command when you change Markdown files.

## Documentation and change records

- Review the docs and release notes for each change.
- Add notable changes to `CHANGELOG.md` under `[Unreleased]`; keep its format and sections.
- Document migration steps for breaking changes.
- Update `README.md` and affected `docs/` files when behavior or requirements change.
- Update examples, setup steps, and configuration docs affected by the change.
- Keep pull request release notes consistent with the changelog.
- Do not edit released changelog entries.
- Change release versions only when the task requires a release.

## Operational conventions

| Concern | Rule |
|---|---|
| Warnings | .NET and TypeScript warnings fail checks; do not suppress them without justification |
| Commit messages | Conventional: `feat:`, `fix:`, `docs:`, `chore:`, `refactor:`, `test:` |

## Permission Boundaries

**Allowed:** edit source, run checks, and add or update tests.
**Ask first:** install packages, delete files, push to shared branches, or change infrastructure.
