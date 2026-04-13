# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- Use `vars.AZURE_LOCATION` with fallback to `inputs.AZURE_LOCATION` in provision and validate workflows.

### Fixed

- Handle null collections for suggestions and paragraphs when creating document summaries.
- Allow empty `ParagraphId` in `Suggestion` model for backward compatibility with legacy data.

### Added

- Unit tests for document summary handling with legacy null collections.
- Serialization tests for `Suggestion` model deserialization behavior.

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
