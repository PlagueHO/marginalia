# Standard Repository Structure

> Confidence: high
> Source: User directive — standard structure for all Daniel's repos, based on PlagueHO/prompt-babbler

## Pattern

All projects follow this folder structure convention, adapted from `PlagueHO/prompt-babbler`.

### Root Layout

```text
{project-name}-app/               # React frontend (Vite, TypeScript, shadcn/ui)
{project-name}-service/            # .NET backend (Clean Architecture)
.vscode/                           # VS Code settings, extensions, MCP servers, tasks
.github/                           # GitHub config
  copilot-instructions.md
  agents/
  instructions/
  prompts/
  workflows/
.devcontainer/                     # Dev container config
docs/                              # Documentation
infra/                             # Azure Bicep infrastructure
.gitignore                         # Combined .NET + Node.js ignores
.gitattributes
GitVersion.yml                     # SemVer from git history
package.json                       # Root pnpm workspace / tooling
.markdownlint.json
.markdownlint-cli2.jsonc
azure.yaml                         # Azure Developer CLI (azd)
aspire.config.json                 # .NET Aspire config
```

### Frontend (`{project-name}-app/`)

```text
src/
  App.tsx
  main.tsx
  index.css
  assets/
  components/                      # UI components (shadcn/ui in components/ui/)
  hooks/                           # Custom React hooks
  pages/                           # Page-level components
  services/                        # API client code
  types/                           # TypeScript type definitions
  lib/                             # Utility functions
tests/                             # Vitest test files
public/                            # Static assets
package.json
tsconfig.json / tsconfig.app.json / tsconfig.node.json
vite.config.ts
vitest.config.ts
vitest.setup.ts
eslint.config.js
components.json                    # shadcn/ui config
```

**Frontend conventions:**

- React 19, hooks only, no class components
- TypeScript strict mode
- Vite dev server, pnpm package manager
- shadcn/ui (New York style) + Radix UI + TailwindCSS v4
- Vitest + @testing-library/react + jest-axe
- Path alias: `@/*` → `./src/*`

### Backend (`{project-name}-service/`)

Clean Architecture with strict dependency direction:

```text
src/
  Api/                             # ASP.NET Core controllers, middleware, DI registration
  Domain/                          # Business models (records), interfaces. No external deps.
  Infrastructure/                  # Service implementations (Azure, DB, AI)
  Orchestration/                   # .NET Aspire AppHost + ServiceDefaults
tests/
  unit/                            # MSTest + NSubstitute + FluentAssertions
  integration/                     # Aspire testing harness
Directory.Build.props              # Shared MSBuild properties (net10.0, nullable, strict)
Directory.Packages.props           # Central package management
{ProjectName}.slnx                 # Solution file
global.json                        # SDK version pinning
.dockerignore
```

**Backend conventions:**

- .NET 10, C#, nullable reference types enabled
- `TreatWarningsAsErrors` and `EnforceCodeStyleInBuild` enabled
- System.Text.Json with CamelCase — never Newtonsoft.Json
- Record types with `required` and `init` properties for domain models
- `[JsonPropertyName("camelCase")]` on all serialized properties
- MSTest SDK 4.1, FluentAssertions, NSubstitute
- Microsoft Agent Framework for AI/LLM integration where applicable

### BYO Model Pattern

The LLM endpoint is user-configurable:

- Frontend: user can provide Foundry Models endpoint URL and API key in the UI
- Environment variables: `FOUNDRY_ENDPOINT` and `FOUNDRY_API_KEY` (or similar)
- Backend reads from either config source; environment variables take precedence

## When to Apply

- Every new project scaffolding task
- Folder names use the pattern `{project-name}-app` and `{project-name}-service`
- For Marginalia: `marginalia-app/` and `marginalia-service/`
