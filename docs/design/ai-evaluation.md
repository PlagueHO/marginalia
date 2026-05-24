---
title: AI Evaluation Suite
description: Run AI-powered quality evaluations for the FoundrySuggestionService to ensure suggestions meet quality standards. Execute evaluations locally or in CI/CD pipelines.
author: Marginalia Team
ms.date: 2026-05-18
ms.topic: how-to
keywords:
  - evaluation
  - ai evaluation
  - quality assurance
  - llm judge
  - foundry
  - microsoft evaluations sdk
estimated_reading_time: 6
---

## Overview

Marginalia includes an AI evaluation suite to continuously validate that `FoundrySuggestionService` meets quality standards. The evaluation tests perform both structural validation (non-AI) and quality judgments (LLM-based) on document suggestions.

The suite is implemented as an MSTest project using the [Microsoft.Extensions.AI.Evaluation](https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.AI.Evaluation) stack and can run:

- **In-process**: Test `FoundrySuggestionService` directly (fastest, no network)
- **Deployed API canary**: Test the full deployed service including document intake and API contracts
- **In CI/CD**: Automatically as part of the e2e-test GitHub Actions workflow
- **Locally**: On demand from the command line

## Quick Start

### Prerequisites

1. **Azure Foundry project endpoint** (`AI_EVAL_FOUNDRY_PROJECT_ENDPOINT`)
   - Format: `https://<your-project>.services.ai.azure.com/api/projects/<project-name>`

1. **Model deployment name** (`FOUNDRY_MODEL_NAME`)
   - The name of a deployed model in your Foundry project (used for LLM-judge quality evaluations)

1. **For deployed canary tests** (optional):
   - `AI_EVAL_API_BASE_URL` — Deployed API hostname (e.g., `https://api.example.com`)
   - `AI_EVAL_ACCESS_CODE` — Access code if environment is protected

### Run In-Process Evaluations

In-process tests only require a Foundry project endpoint and model name. They run the suggestion service directly in-memory.

```powershell
$env:AI_EVAL_FOUNDRY_PROJECT_ENDPOINT = "https://<your-project>.services.ai.azure.com/api/projects/<project-name>"
$env:FOUNDRY_MODEL_NAME = "<deployment-name>"
$env:AI_EVAL_STORAGE_ROOT = "$PWD\marginalia-service\TestResults\AiEvaluationStorage"

dotnet test .\marginalia-service\tests\evaluation\Ai.EvaluationTests\Marginalia.Ai.EvaluationTests.csproj --filter TestCategory=AIEvalInProcess
```

### Run Deployed Canary Tests

To include the full deployed API canary (which tests document upload and API suggestions), also set the API URL and optional access code:

```powershell
$env:AI_EVAL_FOUNDRY_PROJECT_ENDPOINT = "https://<your-project>.services.ai.azure.com/api/projects/<project-name>"
$env:FOUNDRY_MODEL_NAME = "<deployment-name>"
$env:AI_EVAL_API_BASE_URL = "https://<deployed-api>.azurewebsites.net"
$env:AI_EVAL_ACCESS_CODE = "<optional-access-code>"
$env:AI_EVAL_STORAGE_ROOT = "$PWD\marginalia-service\TestResults\AiEvaluationStorage"

dotnet test .\marginalia-service\tests\evaluation\Ai.EvaluationTests\Marginalia.Ai.EvaluationTests.csproj
```

### Generate an HTML Report

After evaluation tests complete, generate a human-readable HTML report:

```powershell
dotnet tool restore
dotnet aieval report -p "$env:AI_EVAL_STORAGE_ROOT" -o "$env:AI_EVAL_STORAGE_ROOT\report.html"
```

Open `report.html` in a browser to view detailed metrics and results.

## Evaluation Types

The suite includes two categories of evaluators:

### Structural Evaluators (Non-AI)

These run automatically and validate the internal consistency and completeness of suggestions:

| Evaluator | Purpose |
|---|---|
| Paragraph Mapping | Ensures all suggested paragraphs exist in the source document |
| Unique Targets | Verifies each paragraph has at most one suggestion |
| Complete Fields | Checks that every suggestion has rationale and proposed change |
| Expected Coverage | Validates suggestions align with guidance provided to the service |
| Meaningful Rewrites | Confirms proposed changes differ meaningfully from original text |

### Quality Evaluators (LLM-Judge)

These use an LLM (via your Foundry project) to assess suggestion quality. They run only if a Foundry endpoint is configured:

| Evaluator | Purpose |
|---|---|
| Relevance | Is the suggestion relevant to the guidance and the document? |
| Coherence | Is the suggested rewrite coherent and grammatically sound? |

LLM-judge evaluators return scores between 0.0 (worst) and 1.0 (best).

## Environment Variables

| Variable | Required | Purpose | Example |
|---|---|---|---|
| `AI_EVAL_FOUNDRY_PROJECT_ENDPOINT` | ✅ | Foundry project endpoint URL | `https://example.services.ai.azure.com/api/projects/proj-123` |
| `FOUNDRY_MODEL_NAME` | ✅ | Deployed model name | `gpt-4o` |
| `AI_EVAL_API_BASE_URL` | ❌ | Deployed API base URL (for canary tests) | `https://api.example.com` |
| `AI_EVAL_ACCESS_CODE` | ❌ | Access code for protected APIs | (varies by environment) |
| `AI_EVAL_STORAGE_ROOT` | ❌ | Evaluation output directory | `$PWD\marginalia-service\TestResults\AiEvaluationStorage` |
| `AI_EVAL_EXECUTION_NAME` | ❌ | Run identifier (for reports) | Defaults to ISO 8601 timestamp |
| `AI_EVAL_USER_ID` | ❌ | User context for API tests | Defaults to `"ai-eval"` |
| `AI_EVAL_ENABLE_CACHE` | ❌ | Cache LLM responses (useful for dev) | `true` or `false`; defaults to `false` |

## CI/CD Integration

The evaluation suite runs automatically in the `ai-evaluation` job of the e2e-test GitHub Actions workflow (`.github/workflows/e2e-test.yml`). The job:

1. Resolves the model deployment name from infrastructure
1. Runs both in-process and deployed canary evaluations
1. Publishes test results via TRX format
1. Generates and uploads the HTML report
1. Adds job summary metadata to the GitHub Actions run

### Artifacts

On CI completion, the following artifacts are available for download:

- **Evaluation test results** (TRX format) for integration with test dashboards
- **HTML report** with detailed metrics, scenario results, and evaluator scores
- **Full evaluation storage** for offline analysis

### Example: View CI Artifacts

1. Go to the e2e-test workflow run in GitHub Actions
1. Scroll to **Artifacts** section
1. Download `ai-evaluation-artifacts`
1. Extract and open `report.html`

## Troubleshooting

### "Unauthorized" or "403 Forbidden" from Foundry

Check that:

- `AI_EVAL_FOUNDRY_PROJECT_ENDPOINT` is correct and reachable
- Your Azure credentials are current (`az account get-access-token`)
- The model name in `FOUNDRY_MODEL_NAME` exists in your Foundry project

### "Connection refused" for API canary

If running deployed canary tests locally:

- Verify `AI_EVAL_API_BASE_URL` is reachable from your machine
- Check network and firewall rules
- Confirm `AI_EVAL_ACCESS_CODE` is correct (if required)

### No LLM evaluations running

Quality evaluations only run if `AI_EVAL_FOUNDRY_PROJECT_ENDPOINT` is set. Check the test output logs:

```text
Structural evaluations completed. Skipping quality evaluations (no Foundry endpoint configured).
```

If you want LLM-judge results, set the environment variables and re-run.

### HTML report not generated

Ensure `dotnet tool restore` ran before `dotnet aieval report`:

```powershell
dotnet tool restore
dotnet aieval report -p "$env:AI_EVAL_STORAGE_ROOT" -o "$env:AI_EVAL_STORAGE_ROOT\report.html"
```

If the command fails, verify the storage root path exists and contains evaluation results.

## Next Steps

- **Expand scenarios**: Add more test cases covering edge cases and tone variations in [marginalia-service/tests/evaluation/Ai.EvaluationTests/Scenarios](../../marginalia-service/tests/evaluation/Ai.EvaluationTests/Scenarios)
- **Tune LLM prompts**: Refine quality evaluator criteria in [SuggestionQualityEvaluators.cs](../../marginalia-service/tests/evaluation/Ai.EvaluationTests/SuggestionQualityEvaluators.cs) for better signal
- **Define quality gates**: Establish minimum thresholds (e.g., Relevance ≥ 0.8) and implement CI workflow logic to fail on substandard results
- **Monitor over time**: Track evaluation metrics across model versions and updates to detect regressions

## References

- [Microsoft.Extensions.AI.Evaluation on GitHub](https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.AI.Evaluation)
- [Testing Guide](./testing.md) — Overview of all Marginalia test suites
- [Local Development](../quickstart-local.md) — Setting up local Foundry credentials and model access
