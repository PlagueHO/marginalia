---
title: Testing Guide
description: Automated testing strategy for Marginalia, including frontend tests, backend unit and integration tests, smoke validation, and coverage reporting.
author: Marginalia Team
ms.date: 2026-05-17
ms.topic: how-to
keywords:
  - testing
  - unit tests
  - integration tests
  - smoke tests
  - vitest
  - mstest
  - aspire
estimated_reading_time: 8
---

## Automated Testing at a Glance

| Layer            | Test type                    | Primary scope                                                          | Runner                                   | Key entry points                                                                                                                         |
|------------------|------------------------------|------------------------------------------------------------------------|------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------|
| Frontend         | Unit and component tests     | React components, hooks, services, and utility functions               | Vitest + Testing Library                 | [marginalia-app/package.json](../marginalia-app/package.json), [marginalia-app/vitest.config.ts](../marginalia-app/vitest.config.ts) |
| Frontend         | Accessibility-focused tests  | `jest-axe` assertions for selected UI components                       | Vitest + jest-axe                        | [marginalia-app/tests/components](../marginalia-app/tests/components), [marginalia-app/vitest.setup.ts](../marginalia-app/vitest.setup.ts) |
| Backend          | Unit tests                   | API controllers, domain models, repositories, services, health checks  | MSTest v4 + FluentAssertions + NSubstitute | [marginalia-service/tests/unit](../marginalia-service/tests/unit), [marginalia-service/tests/unit/Marginalia.Tests.Unit.csproj](../marginalia-service/tests/unit/Marginalia.Tests.Unit.csproj) |
| Backend          | Integration tests            | ASP.NET Core API pipeline behavior and AppHost resource composition    | MSTest v4 + WebApplicationFactory + Aspire testing | [marginalia-service/tests/integration](../marginalia-service/tests/integration), [marginalia-service/tests/integration/Orchestration.IntegrationTests](../marginalia-service/tests/integration/Orchestration.IntegrationTests) |
| End-to-end smoke | Deployed service checks      | API and frontend readiness and critical endpoint behavior              | Pester (PowerShell)                      | [tests/smoke/Smoke.Tests.ps1](../tests/smoke/Smoke.Tests.ps1)                                                                           |

## Frontend Automated Tests

### What is covered

* Component behavior and rendering
* Hook behavior and state transitions
* Service-level logic
* Utility functions
* Accessibility assertions for selected components

Examples:

* Components: [marginalia-app/tests/components](../marginalia-app/tests/components)
* Hooks: [marginalia-app/tests/hooks](../marginalia-app/tests/hooks)
* Services: [marginalia-app/tests/services](../marginalia-app/tests/services)
* Utilities: [marginalia-app/tests/lib](../marginalia-app/tests/lib)

### How to run

From [marginalia-app](../marginalia-app):

1. Install dependencies.

```bash
pnpm install
```

1. Run all frontend tests once.

```bash
pnpm test
```

1. Run in watch mode.

```bash
pnpm test:watch
```

1. Run accessibility-focused tests only.

```bash
pnpm test -t accessibility
```

Equivalent VS Code tasks are defined in [.vscode/tasks.json](../.vscode/tasks.json):

* app: test
* app: test (watch)
* app: test (accessibility)
* app: test (ui)

> [!NOTE]
> The current [marginalia-app/package.json](../marginalia-app/package.json) does not define a `test:ui` script. The `app: test (ui)` task in [.vscode/tasks.json](../.vscode/tasks.json) will require adding that script before it can run successfully.

### Mocks and fakes used

* Vitest module mocking with `vi.mock` and `vi.mocked` for hook and service dependencies
* JSDOM as the test browser environment
* Shared setup in [marginalia-app/vitest.setup.ts](../marginalia-app/vitest.setup.ts), including `jest-dom`, `jest-axe`, and stubs for `matchMedia` and `ResizeObserver`

### Dependencies needed

* Node.js and pnpm
* Frontend dependencies from [marginalia-app/package.json](../marginalia-app/package.json)
* No Docker requirement for frontend unit and component tests

## Backend Unit Tests

### What is covered

* API controller behavior and middleware behavior
* Domain model and contract behavior
* Repository behavior, including in-memory repository contracts
* Infrastructure service behavior
* Health checks

Test project:

* [marginalia-service/tests/unit/Marginalia.Tests.Unit.csproj](../marginalia-service/tests/unit/Marginalia.Tests.Unit.csproj)

### How to run

From [marginalia-service](../marginalia-service):

1. Restore.

```bash
dotnet restore Marginalia.slnx
```

1. Run the unit test project.

```bash
dotnet test --project tests/unit/Marginalia.Tests.Unit.csproj --configuration Release --no-restore --results-directory ./TestResults --coverage --coverage-output-format cobertura --report-trx
```

Recommended task:

* service: test (unit) in [.vscode/tasks.json](../.vscode/tasks.json)

### Mocks and fakes used

* NSubstitute for mocking and substitutions
* FluentAssertions for expressive assertions

### Dependencies needed

* .NET SDK 10
* MSTest v4 on Microsoft Testing Platform as configured in [marginalia-service/global.json](../marginalia-service/global.json)
* NuGet package versions centrally managed in [marginalia-service/Directory.Packages.props](../marginalia-service/Directory.Packages.props)
* No Docker requirement for backend unit tests

## Backend Integration Tests

### What is covered

* API endpoint behavior through the ASP.NET Core pipeline
* User identity extraction from `X-User-Id` and anonymous fallback behavior
* Export pipeline behavior, including download cleanup of temporary artifacts
* Aspire AppHost resource composition checks for orchestration

Test projects:

* [marginalia-service/tests/integration/Marginalia.Tests.Integration.csproj](../marginalia-service/tests/integration/Marginalia.Tests.Integration.csproj)
* [marginalia-service/tests/integration/Orchestration.IntegrationTests/Marginalia.Orchestration.IntegrationTests.csproj](../marginalia-service/tests/integration/Orchestration.IntegrationTests/Marginalia.Orchestration.IntegrationTests.csproj)

### How to run

From [marginalia-service](../marginalia-service):

1. Run the API integration test project.

```bash
dotnet test --project tests/integration/Marginalia.Tests.Integration.csproj --configuration Release --no-restore --results-directory ./TestResults
```

1. Run orchestration integration tests.

```bash
dotnet test --project tests/integration/Orchestration.IntegrationTests/Marginalia.Orchestration.IntegrationTests.csproj --configuration Release --no-restore --results-directory ./TestResults
```

VS Code tasks:

* service: test (integration)
* service: test (integration - Orchestration)

Defined in [.vscode/tasks.json](../.vscode/tasks.json).

### Mocks and fakes used

API integration tests use a hybrid approach:

* Real in-process host through `WebApplicationFactory`
* In-memory repository substitutions for Cosmos-backed interfaces
* No-op `IChatClient` substitution to avoid requiring live AI Foundry during API integration runs

Orchestration integration tests use Aspire testing to inspect the distributed application model defined in AppHost.

### Dependencies needed

* .NET SDK 10
* For API and orchestration integration tests in this repo, Docker is not required by default because tests use in-process hosting and substitutions
* Integration and orchestration assemblies are currently configured with method-level parallelization:
  * [marginalia-service/tests/integration/MSTestSettings.cs](../marginalia-service/tests/integration/MSTestSettings.cs)
  * [marginalia-service/tests/integration/Orchestration.IntegrationTests/AssemblyInfo.cs](../marginalia-service/tests/integration/Orchestration.IntegrationTests/AssemblyInfo.cs)

## Smoke Tests

### What is covered

Smoke tests validate deployed environment readiness and core API and frontend routes:

* API `/health` and `/alive`
* Documents API access behavior, including optional access-code enforcement checks
* Frontend root page and static JavaScript asset serving

Reference:

* [tests/smoke/Smoke.Tests.ps1](../tests/smoke/Smoke.Tests.ps1)

### How to run

Run the PowerShell smoke script with required base URLs and optional access code.

```powershell
pwsh ./tests/smoke/Smoke.Tests.ps1 -ApiBaseUrl "https://<api-host>" -FrontendBaseUrl "https://<frontend-host>" -AccessCode "<optional-code>"
```

### Mocks and fakes used

* No mocks
* These tests execute against live, reachable endpoints

### Dependencies needed

* PowerShell with Pester support
* Reachable API and frontend URLs
* Access code only when the target environment enforces one

## Coverage and Reporting

Backend coverage workflow:

1. Run unit tests with Cobertura output using service: test (unit).
1. Generate reports using service: code coverage (report).

Task definitions are in [.vscode/tasks.json](../.vscode/tasks.json).

## Practical Notes

* `service: test` runs solution tests with `--no-build` and depends on `service: build`.
* The unit test task includes `--coverage`, Cobertura output, and `--report-trx`, making it the canonical command for backend coverage data in this repo.
