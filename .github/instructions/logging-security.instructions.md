---
description: "Logging security guidance for backend C# code"
applyTo: "marginalia-service/src/**/*.cs"
---

# Logging Security Instructions

Follow these rules for all backend logging:

* Do not log raw user-derived text such as document content, transcription content, prompt content, guidance content, or user-authored titles.
* Do not log access-code values, query-string access-code values, tokens, secrets, or credential material.
* Do not log file names or file paths from request-driven flows.
* Prefer metadata-only logs: counts, lengths, durations, booleans, lifecycle events, and stable entity identifiers.
* Keep `UserId` logging only where needed for operational diagnostics.

Centralized defense in depth is required:

* Keep `marginalia-service/src/Orchestration/ServiceDefaults/Logging/LogSanitizer.cs` active.
* Keep `marginalia-service/src/Orchestration/ServiceDefaults/Logging/SanitizingLogRecordProcessor.cs` registered in `ConfigureOpenTelemetry()`.

For AI and analysis flows:

* Log paragraph counts, suggestion counts, and completion status.
* Do not log prompt payloads, guidance payloads, or analyzed text payloads.

For access-control flows:

* Log generic outcomes with safe request metadata such as HTTP method and route.
* Never include secret values in placeholders or structured arguments.
