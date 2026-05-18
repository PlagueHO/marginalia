# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.2.2] - 2026-05-18

### Changed

- Removed the `AZURE_STATIC_WEB_APP_CUSTOM_DOMAIN` secret from the production deployment workflow.

## [1.2.1] - 2026-05-18

### Fixed

- Restored `AZURE_STATIC_WEB_APP_CUSTOM_DOMAIN` secret to the validate and provision
  CI/CD jobs, which was inadvertently removed in the previous release.

## [1.2.0] - 2026-05-18

### Added

- `Marginalia.Tools.Cli` — a new command-line tool for importing and exporting documents
  via the Marginalia API, with job status polling and unit test coverage.
- Mode-aware user scoping for bulk import/export: single-user mode exports/imports all
  manuscripts while multi-user mode is restricted to the current user's data. Includes
  import normalization to prevent cross-user data leakage.
- GitHub App scripts for automation support.

### Changed

- Removed custom domain secret from deployment configuration.
- Normalized markdown documentation filename casing.
- Updated package versions across project files.

## [1.1.10] - 2026-05-10

### Changed

- Refined custom domain handling in deployment templates.

## [1.1.9] - 2026-05-10

### Added

- Dynamic page titles that reflect the active page and selected item context.
- User-facing handling for AI content filter blocks with structured API errors and categorized trigger details.

### Changed

- Added support for custom domains in Azure Static Web App deployment.
- Refreshed documentation and visuals, including architecture and how-it-works diagrams, deployment guidance, and configuration notes.
- Updated workflow actions, container image references, and template hashes used by deployment automation.

### Fixed

- Improved content filter log formatting and expanded related test and type export coverage.

### Dependencies

- Aligned Aspire package versions in `marginalia-service` to 13.2.4.

### Removed

- Outdated GitHub workflow files.
- Accidentally committed `package-lock.json`; added ignore rules to prevent reintroduction.

## [1.1.8] - 2026-04-13

### Added

- `ContentFilterException` domain exception to represent Azure OpenAI content filter rejections with per-category filter results.
- Content filter detection in `FoundrySuggestionService` via `TryParseContentFilterResults()` method.
- HTTP 422 Unprocessable Entity response in `DocumentsController` when content filter is triggered, with structured error payload including triggered categories.
- Content filter error handling in `EditorPage` with user-friendly toast messaging distinguishing content safety blocks from other failures.
- Support for `error` field in API error response parsing (`api.ts`).
- `ContentFilterCategory` interface and unit tests for content filter error handling.
- GitHub issue templates for bug reports, chores, and feature requests.
- Unit tests for document summary handling with legacy null collections.
- Serialization tests for `Suggestion` model deserialization behavior.

### Changed

- Use `vars.AZURE_LOCATION` with fallback to `inputs.AZURE_LOCATION` in provision and validate workflows.

### Fixed

- Handle null collections for suggestions and paragraphs when creating document summaries.
- Allow empty `ParagraphId` in `Suggestion` model for backward compatibility with legacy data.

## [1.1.7] - 2026-04-13

### Added

- Document deletion feature with API endpoint, `DeleteConfirmationDialog` component, and unit tests.
- Unit tests for `DocumentsController` suggestion status updates.
- `rejectedCount` to suggestion state management and props.
- `buildSummaryMessage` function for alert summaries, extracted to a separate file.
- Merge accepted suggestions into paragraphs during analysis.
- Summary display in `ReplaceAnalysisConfirmationDialog`.
- Unit tests for `SuggestionMergeService` and `WordDocumentService`.

### Changed

- Refactor `Document` model to use `Paragraphs` instead of `Content`.
- Refactor `Suggestion` model to reference `ParagraphId` instead of `TextRange`.
- Refactor `SuggestionUpdateRequest` to use `userSteeringInput` instead of `modifiedText`.
- Update `useSuggestions` hook to reflect changes in suggestion status handling.
- Update `WordDocumentService` to apply suggestions based on new status logic.
- Add `DocumentFormat.OpenXml` project dependency.

### Removed

- `TextRangeTests` (no longer needed after model refactor).

## [1.1.6] - 2026-04-12

### Added

- Document title renaming feature with API endpoint and `UpdateDocumentTitleRequest` model.
- Theme toggle functionality in the app header.
- Tooltip for accepted suggestions in the document editor.
- Resizable panels in the main layout.
- Telemetry for API error tracking.

### Changed

- Replace `Loader2` icons with a new `Spinner` component for consistency across the app.
- Introduce `gradientText` and `mutedText` utility classes for text styling.
- Enhance `FoundrySuggestionService` to support structured outputs with a JSON schema.
- Refactor role assignment logic to ignore empty principal IDs in Cosmos DB and Foundry resources.
- Update Bicep and JSON templates to support nullable principal IDs.
- Update badge styles for suggestion statuses.
- Update package versions in `marginalia-service`.

### Fixed

- New favicon design.

## [1.1.5] - 2026-04-11

### Added

- .NET 10 backend with ASP.NET Core API and .NET Aspire orchestration
- React 19 frontend with TypeScript, Vite, and shadcn/ui
- Document upload, analysis, and annotation workflow
- AI-powered editorial suggestions via Azure AI Foundry
- Cosmos DB document and session repositories
- Azure Bicep infrastructure as code
- OpenTelemetry instrumentation for frontend and backend
- Smoke tests and CI/CD workflows
