# Project Context

- **Owner:** Mary MacGregor-Reid
- **Project:** Marginalia — AI-powered narrative flow editor for long-form non-fiction writers. Helps authors refine, expand, and ensure stylistic consistency in manuscripts with AI-generated suggestions and fine-grained editorial control.
- **Stack:** React (frontend), .NET 10 / ASP.NET (backend), Azure Functions, Azure AI / Microsoft Foundry, Google Docs API integration
- **Created:** 2026-03-22

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-03-22 — Initial Project Structure

**Architecture:** Clean Architecture with strict dependency direction. Domain has zero external dependencies — only System.Text.Json attributes. Infrastructure depends on Domain. Api depends on both.

**Backend structure:**

- `marginalia-service/src/Domain/Models/` — Record types: Document, Suggestion, UserSession, TextRange, enums (SuggestionStatus, DocumentSource)
- `marginalia-service/src/Domain/Interfaces/` — ISuggestionService, IDocumentRepository
- `marginalia-service/src/Domain/Configuration/` — LlmEndpointOptions (BYO model pattern)
- `marginalia-service/src/Api/` — ASP.NET Core Web API
- `marginalia-service/src/Infrastructure/` — Service implementations (empty, ready for wiring)
- `marginalia-service/tests/unit/` and `tests/integration/` — MSTest projects with NSubstitute + FluentAssertions
- Central package management via `Directory.Packages.props`
- Shared build props via `Directory.Build.props` (net10.0, nullable, TreatWarningsAsErrors, EnforceCodeStyleInBuild)

**Frontend structure:**

- `marginalia-app/` — React 19 + Vite 8 + TypeScript (strict mode)
- TailwindCSS v4 via `@tailwindcss/vite` plugin
- shadcn/ui (New York style) configured via `components.json`
- Path alias `@/*` → `./src/*` in both tsconfig and vite config
- Vitest + @testing-library/react + jest-axe for testing
- `src/lib/utils.ts` — cn() utility for class merging (clsx + tailwind-merge)

**BYO Model pattern:** LlmEndpointOptions config class reads from `LlmEndpoint` config section. Environment variables FOUNDRY_ENDPOINT, FOUNDRY_API_KEY, FOUNDRY_MODEL_NAME override config. Frontend can also supply endpoint/key via UI.

**Key decisions:**

- MSTest 4.1.0 requires Microsoft.NET.Test.Sdk >= 18.0.1 (not 17.x)
- Domain models use `required init` properties with `[JsonPropertyName]` for camelCase serialization
- All enums use `[JsonConverter(typeof(JsonStringEnumConverter))]` for string serialization

### 2026-03-22 — Quickstart Guides Created

**Docs added:**

- `docs/QUICKSTART-LOCAL.md` — Complete local dev guide using .NET Aspire, covering prerequisites, Azure credential setup, first-run walkthrough, model config overrides, tests, and troubleshooting.
- `docs/QUICKSTART-AZURE.md` — Placeholder for Azure Developer CLI deployment (not yet implemented — no `azure.yaml` or Bicep templates exist). Documents the intended workflow for future implementation.

**Key architectural details captured:**

- AppHost provisions Azure AI Foundry (`ai-foundry`) with `gpt-5.3-chat` chat deployment (GlobalStandard SKU, capacity 50)
- Model name/version configurable via `MicrosoftFoundry:chatModelName` / `MicrosoftFoundry:chatModelVersion` user secrets or env vars
- Default Azure region: `swedencentral`, credential source: `AzureCli`
- No Docker containers required (unlike prompt-babbler reference project)
- No authentication layer (no Entra ID)
- Frontend uses pnpm, backend tests via `dotnet test Marginalia.slnx`

**Pattern:** Quickstart docs follow PlagueHO/prompt-babbler structure and tone. Cross-linked between local and Azure guides.

### 2026-03-22 — README Restructured to Product Format

**Changes made:**

- Restructured README.md to match prompt-babbler template: product-focused one-liner → What It Does (workflow) → Key Features → Quick Start → Documentation table → License
- Moved all spec content (functional/non-functional requirements, data model, architecture, constraints) into a collapsible `<details>` section at the bottom
- One-paragraph summary now describes Marginalia as a product for writers seeking narrative polish with AI assistance, not as a specification
- "What It Does" section walks through user workflow: Upload → Guidance → Analyze → Review → Export
- "Key Features" emphasizes writer benefits and tech stack details (React 19, Vite, shadcn/ui, .NET 10, Aspire, Microsoft Foundry)
- Quick Start references existing docs/QUICKSTART-LOCAL.md and docs/QUICKSTART-AZURE.md
- Prerequisites section lists all dependencies: .NET 10, Node.js 22+, pnpm, Aspire CLI, Azure CLI, Azure Account
- Run Locally section shows: git clone → cd → az login → aspire run
- Documentation table lists Local Development and Deploy to Azure docs
- License badge added (MIT); no CI/CD badges (workflows not yet implemented)
- Specification section contains all original PRD content (FRs, NFRs, tech stack, architecture, data model, constraints)

**Outcome:** README now presents Marginalia as a finished product to visitors while preserving full technical specification for developers.
