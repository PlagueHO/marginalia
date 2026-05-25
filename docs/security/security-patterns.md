---
title: Security Patterns
description: Logging safety and sensitive data handling patterns for Marginalia backend services.
---

## Logging Safety Pattern

Marginalia uses a defense-in-depth logging safety model to reduce log injection risk (CWE-117) and avoid exposing sensitive payloads.

The model has two layers:

1. Centralized sanitization in ServiceDefaults OpenTelemetry logging.
1. Call-site hardening that avoids logging raw sensitive values.

## Centralized Sanitization

The backend registers a shared OpenTelemetry log processor in ServiceDefaults:

* `marginalia-service/src/Orchestration/ServiceDefaults/Logging/SanitizingLogRecordProcessor.cs`
* `marginalia-service/src/Orchestration/ServiceDefaults/Logging/LogSanitizer.cs`

The sanitizer escapes control characters in:

* `FormattedMessage`
* string `Body`
* string-valued log attributes

Escaping behavior:

* `\r` becomes `\\r`
* `\n` becomes `\\n`
* `\t` becomes `\\t`
* other control characters become `\\uXXXX`

## Call-Site Hardening Rules

Do not log these values directly:

* document or prompt text
* user guidance content
* access-code values
* file names
* file paths
* user-authored titles

Use metadata-only logging instead:

* counts
* durations
* booleans
* lengths
* lifecycle events
* stable identifiers

## CI Validation Gate

The backend workflow includes a logging safety check:

* `.github/scripts/Test-LoggingSafety.ps1`
* `.github/workflows/build-backend-service.yml`

The check scans backend API and infrastructure logger calls for sensitive placeholder and argument patterns and fails CI when matches are found.

## Review Guidance

Before merging backend changes:

1. Verify new log statements do not include sensitive payload values.
1. Keep the centralized sanitizing processor enabled.
1. Ensure CI logging safety validation passes.

Logging changes are implemented with accessibility in mind, but accessibility issues may still exist and should be manually reviewed and tested with tooling such as Accessibility Insights.
