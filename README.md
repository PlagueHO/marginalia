# Marginalia

[![CI][ci-shield]][ci-url]
[![CD][cd-shield]][cd-url]
[![License: MIT][license-shield]][license-url]

Marginalia is an AI-powered narrative flow editor for long-form non-fiction writers. Upload your manuscript, add editorial guidance, and receive intelligent suggestions for expanding compressed sections, polishing style, and strengthening narrative flow. Accept, reject, or refine suggestions before exporting—all while maintaining your unique authorial voice.

## What It Does

Marginalia guides you through a collaborative editorial workflow with AI-powered insights at every step:

1. **Upload or Paste** — Import your Word document or paste text directly into the editor.
2. **Provide Guidance** — Specify areas for improvement (compressed narrative, AI-like writing, tone adjustments) or select specific text.
3. **Analyze** — AI analyzes your text and highlights passages where narrative expansion, style refinement, or consistency improvements could help.
4. **Review & Refine** — Read suggestion rationales, modify them if desired, and see edits in real time with visual highlighting.
5. **Accept & Export** — Batch-approve suggestions or fine-tune individual edits, then export your revised manuscript as Word or directly to Google Docs.

### Key Features

- **AI-Powered Suggestions** — Identifies compressed narrative, stylistic inconsistencies, repetitive structures, and opportunities for expansion with transparent rationale.
- **Fine-Grained Control** — Accept, reject, or refine individual suggestions before applying them. Your voice stays yours.
- **Real-Time Editing** — See edits highlighted and applied instantly as you interact with suggestions.
- **Multiple Input Formats** — Upload Word documents or paste text; export to Word locally or to Google Docs directly.
- **Flexible Guidance** — Provide custom instructions per section, choose tone profiles (narrative, professional, academic), or free-form guidance.
- **Built for Writers** — Clean, distraction-free UI with accessibility as a first-class concern. Supports up to 10-page documents per session.
- **Modern Tech Stack** — React 19 + Vite + shadcn/ui frontend, .NET 10 backend, orchestrated with .NET Aspire, powered by Microsoft Foundry AI.
- **Bring Your Own Model** — Configure your own Foundry endpoint and API key (or use Entra ID authentication), or connect your own compatible LLM.

## Quick Start

For full setup instructions, see [docs/QUICKSTART-LOCAL.md](docs/QUICKSTART-LOCAL.md). To deploy to Azure, see [docs/QUICKSTART-AZURE.md](docs/QUICKSTART-AZURE.md).

### Prerequisites to run Locally

- **.NET 10 SDK** — [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Node.js 22+** — [Download](https://nodejs.org/)
- **pnpm** — `npm install -g pnpm` (or [download](https://pnpm.io/installation))
- **.NET Aspire CLI** — `dotnet workload install aspire`
- **Azure CLI** — [Download](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli)
- **Azure Account** — For AI Foundry integration (can use free tier; see docs for details)

### Run Locally

```bash
git clone https://github.com/marymacgregorreid/marginalia.git
cd marginalia
az login
aspire run
```

On first run, Aspire will provision a local AI Foundry environment and start the backend and frontend. Open <http://localhost:5173> to begin editing.

For detailed troubleshooting, prerequisites verification, and model configuration overrides, see [docs/QUICKSTART-LOCAL.md](docs/QUICKSTART-LOCAL.md).

## Documentation

| Document | Description |
| ---------- | ------------- |
| [Local Development](docs/QUICKSTART-LOCAL.md) | Complete local dev setup, Azure credential configuration, first-run walkthrough, and troubleshooting. |
| [Deploy to Azure](docs/QUICKSTART-AZURE.md) | Planned Azure Developer CLI deployment architecture and infrastructure setup (in progress). |

## License

MIT

<!-- Badge reference links -->
[ci-shield]: https://img.shields.io/github/actions/workflow/status/marymacgregorreid/marginalia/continuous-integration.yml?branch=main&label=CI
[ci-url]: https://github.com/marymacgregorreid/marginalia/actions/workflows/continuous-integration.yml
[cd-shield]: https://img.shields.io/github/actions/workflow/status/marymacgregorreid/marginalia/continuous-delivery.yml?branch=main&label=CD
[cd-url]: https://github.com/marymacgregorreid/marginalia/actions/workflows/continuous-delivery.yml
[license-shield]: https://img.shields.io/badge/license-MIT-blue.svg
[license-url]: https://github.com/marymacgregorreid/marginalia/blob/main/LICENSE

---

<details>
<summary><strong>Specification</strong> — Full technical requirements and data model.</summary>

## Specification

### Functional Requirements

#### Document Ingestion & Management

- Upload Microsoft Word documents from the local machine.
- Optionally import/export documents with Google Docs integration.
- Split documents into manageable sections/chunks for editing (e.g., by chapter or up to ~3 pages).
- Allow pasting text into an editor as an alternative to file upload.

#### Guidance & Control

- Let the author specify areas for improvement (e.g., compressed narrative, AI-like writing, lack of color).
- Allow the author to manually select text areas for editorial focus or review.
- Enable authors to provide custom written instructions per section or selection.
- Present the option to choose or describe desired tone adjustments (e.g., professional, narrative, academic), or free-form guidance (typed input).

#### Suggestion Engine

- Analyze the given text, highlighting passages where:
  - The narrative may be overly compressed.
  - Style is inconsistent or "AI-like."
  - Repetitive/awkward prosaic structures are found.
  - Additional narrative detail or expansion could be beneficial.
- Provide AI-generated suggestions with explanation for each.
- Allow batch or individual acceptance/rejection of suggestions.
- Enable users to modify, refine, or further steer the AI's suggestions before applying.

#### User Interface

- Display editable text with visually distinct highlights for suggested areas (e.g., colored highlights).
- Show suggestion rationale (hover or sidebar).
- Checklist or review pane summarizing all pending suggestions.
- Apply selected changes to the document, maintaining original formatting where possible.

#### Export & Save

- Export revised document locally as a Word file.
- Optionally, export/save directly to Google Docs.
- Maintain a copy/history for further revisions if desired.

### Non-Functional Requirements

- Minimal security—no identity management required.
- Reliable file handling for local document imports/exports.
- Responsive and user-friendly interface.
- Support for up to 10-page documents per session.
- Accessible UI for authors with various needs.

### Tech Stack

- **Frontend**: React 19, Vite 8, TypeScript (strict mode), TailwindCSS v4, shadcn/ui (New York style)
- **Backend**: .NET 10, ASP.NET Core Web API, Clean Architecture
- **Database**: In-memory (ConcurrentDictionary) for session persistence; ready for database migration
- **Infrastructure**: Azure, .NET Aspire orchestration, Microsoft Foundry for AI models, Azure Functions for optional orchestration

### Architecture Overview

#### Client-Server Model

- React frontend for UI, text highlighting, and direct user interaction.
- Backend APIs handle file uploads, document parsing, session management.
- Integration with Azure AI services for text analysis and suggestion generation.
- Session-based storage for temporary document versions and suggestions.
- .NET Aspire manages local development and cloud orchestration.

#### Backend Structure (Clean Architecture)

- `Domain/` — Core models (Document, Suggestion, UserSession, TextRange), interfaces, and enums with zero external dependencies.
- `Api/` — ASP.NET Core endpoints for file uploads, document analysis, suggestion management, and AI configuration.
- `Infrastructure/` — Service implementations for document parsing, AI integration, and storage.
- Shared build configuration via `Directory.Build.props` and `Directory.Packages.props`.

#### Frontend Structure

- `src/` — React components organized by domain (document editor, suggestion panel, upload, export).
- Custom hooks for state management (`useDocument`, `useSuggestions`, `useAnalysis`, `useLlmConfig`).
- Component accessibility tested with jest-axe; Suggestion cards use color + icon (not color-only).

### Data Model

#### Document

- `id` — Unique identifier
- `filename` — Original filename
- `source` — Origin (local upload or Google Docs)
- `content` — Raw text or array of sections/chunks
- `suggestions[]` — Array of associated suggestions

#### Suggestion

- `id` — Unique identifier
- `document_id` — Foreign key to Document
- `text_range` — Start and end character positions
- `rationale` — Explanation for the suggestion
- `proposed_change` — Suggested replacement text
- `status` — pending | accepted | rejected | modified
- `user_steering_input` — Author's custom feedback on the suggestion

#### UserSession

- `session_id` — Unique identifier
- `document_ids[]` — Documents opened in this session
- `timestamp` — Session created/updated
- `temp_storage` — Temporary work-in-progress state

### External Integrations

- **Microsoft Foundry / Azure OpenAI** — For AI-powered text analysis and suggestion generation. Supports API key authentication or Entra ID authentication.
- **Google Docs API** — For importing/exporting documents to/from Google Docs.
- **Microsoft Word Compatibility** — For file handling and preservation of formatting.

### Constraints & Assumptions

- Maximum document chunk size is approximately 3 pages (~6000 characters) per analysis to maintain responsiveness.
- Only local storage or Google Docs are supported for output—no OneDrive integration.
- Minimal authentication—BYO model pattern via LLM endpoint configuration.
- Internet connection required for AI processing and cloud integration.
- Original formatting and footnotes use best-effort preservation.
- Designed for English-language manuscripts.
- Suggestions applied in reverse text order during export to preserve character offsets.

</details>
