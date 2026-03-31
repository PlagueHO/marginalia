# Marginalia

## Summary

Narrative Flow Editor Assistant is an AI-powered tool for writers of long-form non-fiction, enabling them to refine, expand, and ensure stylistic consistency in their manuscripts. Designed especially for authors seeking to give their work more narrative "air" and polish without losing their personal voice, the application provides intelligent suggestions for improvements, offers fine-grained control over edits, and allows writers to accept, decline, or further steer recommendations before applying them locally or exporting to Google Docs.

## Goals

- Help writers identify and improve areas of text that are compressed, overly factual, or stylistically inconsistent.
- Maintain and reinforce the author’s unique tone and narrative style.
- Allow authors to steer the editorial process with detailed input and oversight.
- Provide AI-generated suggestions with transparent rationale and user accept/reject control.
- Seamlessly import/export Microsoft Word documents or interact with Google Docs.

## User Roles

- **Author**: Uploads manuscript/document, provides stylistic guidance, interacts with AI suggestions, accepts/rejects/edits changes, and downloads or exports the revised manuscript.

## Functional Requirements

### Document Ingestion & Management

1. Upload Microsoft Word documents from the local machine.
1. Optionally import/export documents with Google Docs integration.
1. Split documents into manageable sections/chunks for editing (e.g., by chapter or up to ~3 pages).
1. Allow pasting text into an editor as an alternative to file upload.

### Guidance & Control

1. Let the author specify areas for improvement (e.g., compressed narrative, AI-like writing, lack of color).
1. Allow the author to manually select text areas for editorial focus or review.
1. Enable authors to provide custom written instructions per section or selection.
1. Present the option to choose or describe desired tone adjustments (e.g., professional, narrative, academic), or free-form guidance (typed input).

### Suggestion Engine

1. Analyze the given text, highlighting passages where:
   - The narrative may be overly compressed.
   - Style is inconsistent or "AI-like."
   - Repetitive/awkward prosaic structures are found.
   - Additional narrative detail or expansion could be beneficial.
1. Provide AI-generated suggestions with explanation for each.
1. Allow batch or individual acceptance/rejection of suggestions.
1. Enable users to modify, refine, or further steer the AI's suggestions before applying.

### User Interface

1. Display editable text with visually distinct highlights for suggested areas (e.g., colored highlights).
1. Show suggestion rationale (hover or sidebar).
1. Checklist or review pane summarizing all pending suggestions.
1. Apply selected changes to the document, maintaining original formatting where possible.

### Export & Save

1. Export revised document locally as a Word file.
1. Optionally, export/save directly to Google Docs.
1. Maintain a copy/history for further revisions if desired.

## Non-Functional Requirements

- Minimal security—no identity management required.
- Reliable file handling for local document imports/exports.
- Responsive and user-friendly interface.
- Support for up to 10-page documents per session.
- Accessible UI for authors with various needs.

## Tech Stack

- **Frontend**: React
- **Backend**: .NET 10 (ASP.NET), possibly Azure Functions for AI service orchestration
- **Database**: (Optional/minimal) local/session-based persistence for uploaded documents
- **Infrastructure**: Hosted on Azure; AI models served via Microsoft Foundry (ChatGPT 5.3-chat or similar)

## Architecture Overview

- **Client-Server Model**:
  - React frontend for UI, text highlighting, and direct user interaction.
  - Backend APIs handle file uploads, document parsing, session management.
  - Integration with Azure AI services for text analysis and suggestion generation.
  - Optional database or storage for temporary document versions.
  - Azure Functions to orchestrate AI interactions and document processing.

## Data Model

- **Document**: id, filename, source (local/Google Docs), content (raw/sections), suggestions[]
- **Suggestion**: id, document_id, text_range (start/end), rationale, proposed_change, status (pending, accepted, rejected, modified), user_steering_input
- **UserSession**: session_id, document_ids[], timestamp, temp_storage

## External Integrations

- **Microsoft Foundry/ChatGPT**: For AI-powered text analysis and suggestion generation.
- **Google Docs API**: For importing/exporting documents to/from Google Docs.
- **Microsoft Word Compatibility**: For file handling and preservation of formatting.

## Constraints & Assumptions

- Maximum document chunk size is approximately 3 pages per analysis to maintain responsiveness.
- Only local storage or Google Docs are supported for output—no OneDrive integration.
- Minimal or no user authentication.
- Internet connection required for AI processing and cloud integration.
- Original formatting/footnotes may be best-effort preserved if not possible in the editor.
- Designed for English-language manuscripts.
