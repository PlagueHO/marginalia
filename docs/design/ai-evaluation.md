---
title: AI Evaluation Suite
description: Run AI-powered quality evaluations for the FoundrySuggestionService to ensure suggestions meet quality standards. Execute evaluations locally or in CI/CD pipelines.
author: Marginalia Team
ms.date: 2026-05-24
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

Marginalia includes an AI evaluation suite that validates suggestion quality for `FoundrySuggestionService` with deterministic structural checks and judge-backed quality checks.

The suite lives in `marginalia-service/tests/evaluation/Ai.EvaluationTests` and is implemented with MSTest plus the `Microsoft.Extensions.AI.Evaluation` reporting pipeline.

Two test modes are implemented:

- `AIEvalInProcess`: Calls `FoundrySuggestionService.AnalyzeAsync` directly
- `AIEvalDeployed`: Calls deployed API endpoints (`/api/documents/paste` and `/api/documents/{id}/analyze`) and remaps returned paragraph IDs back to scenario IDs before evaluation

Both modes execute the same evaluator set and both are included in CI through `TestCategory=AIEval`.

## Quick Start

### Prerequisites

1. Foundry project endpoint
1. Runtime model deployment name
1. Judge model deployment name (optional, defaults to runtime model)
1. For deployed canary tests only: deployed API host and optional access code

Minimum required environment variables:

```powershell
$env:AI_EVAL_FOUNDRY_PROJECT_ENDPOINT = "https://<your-project>.services.ai.azure.com/api/projects/<project-name>"
$env:AI_EVAL_MODEL_NAME = "<runtime-deployment-name>"
```

### Run In-Process Evaluations

These tests run all scenarios in `Scenarios/foundry-suggestion-scenarios.json` against the in-process service path.

```powershell
$env:AI_EVAL_FOUNDRY_PROJECT_ENDPOINT = "https://<your-project>.services.ai.azure.com/api/projects/<project-name>"
$env:AI_EVAL_MODEL_NAME = "<runtime-deployment-name>"
$env:AI_EVAL_JUDGE_MODEL_NAME = "<judge-deployment-name>" # optional
$env:AI_EVAL_STORAGE_ROOT = "$PWD\marginalia-service\TestResults\AiEvaluationStorage"

dotnet test .\marginalia-service\tests\evaluation\Ai.EvaluationTests\Marginalia.Ai.EvaluationTests.csproj --filter TestCategory=AIEvalInProcess
```

### Run Deployed Canary Tests

These tests run only scenarios with `runInDeployedCanary: true`. If no API base URL is configured, the deployed canary test is marked inconclusive.

```powershell
$env:AI_EVAL_FOUNDRY_PROJECT_ENDPOINT = "https://<your-project>.services.ai.azure.com/api/projects/<project-name>"
$env:AI_EVAL_MODEL_NAME = "<runtime-deployment-name>"
$env:AI_EVAL_JUDGE_MODEL_NAME = "<judge-deployment-name>" # optional
$env:AI_EVAL_API_BASE_URL = "https://<deployed-api>.azurewebsites.net"
$env:AI_EVAL_ACCESS_CODE = "<optional-access-code>"
$env:AI_EVAL_STORAGE_ROOT = "$PWD\marginalia-service\TestResults\AiEvaluationStorage"

dotnet test .\marginalia-service\tests\evaluation\Ai.EvaluationTests\Marginalia.Ai.EvaluationTests.csproj --filter TestCategory=AIEvalDeployed
```

### Generate an HTML Report

After evaluation tests complete, generate a human-readable HTML report:

```powershell
dotnet tool restore
dotnet aieval report -p "$env:AI_EVAL_STORAGE_ROOT" -o "$env:AI_EVAL_STORAGE_ROOT\report.html"
```

Open `report.html` in a browser to view detailed metrics and results.

## Implementation Approach

The implementation flow is:

1. Load environment settings from `FoundrySuggestionEvaluationEnvironment.Load()`
1. Load scenario set from `Scenarios/foundry-suggestion-scenarios.json`
1. Create `DiskBasedReportingConfiguration` with structural and quality evaluators
1. Execute scenario through in-process or deployed path
1. Build evaluator input as synthetic chat messages and assistant response rendered from `Suggestion` objects
1. Evaluate with shared evaluator pipeline
1. Assert each required metric reports `Interpretation.Failed == false`

### Judge Client

Quality evaluators run through `FoundryOpenAiChatClient`, which:

- Uses `DefaultAzureCredential`
- Requests token scope `https://ai.azure.com/.default`
- Calls `POST /openai/v1/chat/completions` on the Foundry host

### Scenario Schema

Each scenario supports the following fields:

- `id`
- `description`
- `userGuidance`
- `paragraphs` (`id`, `text`)
- `expectedTargetParagraphIds`
- `allowedTargetParagraphIds`
- `minimumSuggestionCount`
- `maximumSuggestionCount`
- `runInDeployedCanary`

Current dataset version is `v1` and includes two scenarios.

## Evaluations Performed

The suite includes two categories of evaluators:

### Structural Evaluators (Non-AI)

These custom evaluators are implemented in `SuggestionStructureEvaluators.cs`:

| Evaluator | Metric name | Pass condition |
|---|---|---|
| ParagraphMappingEvaluator | `Valid paragraph mapping` | Every suggestion `ParagraphId` exists in scenario paragraphs |
| UniqueParagraphTargetEvaluator | `One suggestion per paragraph` | No paragraph receives more than one suggestion |
| SuggestionFieldsEvaluator | `Complete suggestion fields` | At least one suggestion, and each has non-empty `Rationale` and `ProposedChange` |
| ExpectedCoverageEvaluator | `Expected target coverage` | Required targets covered, no disallowed targets, and suggestion count within scenario bounds |
| MeaningfulRewriteEvaluator | `Meaningful rewrite` | At least one suggestion, and normalized proposed text differs from source paragraph text |

### Quality Evaluators (LLM-Judge)

These are provided by `Microsoft.Extensions.AI.Evaluation.Quality` and run for every evaluation:

| Evaluator | Required metric name |
|---|---|
| RelevanceEvaluator | `Relevance` |
| CoherenceEvaluator | `Coherence` |

The test harness requires both metrics to be present and passing. Metric lookup uses exact-name, case-insensitive, and contains matching to tolerate naming variations.

## Environment Variables

| Variable | Required | Purpose | Fallbacks and defaults |
|---|---|---|---|
| `AI_EVAL_FOUNDRY_PROJECT_ENDPOINT` | ✅ | Foundry project endpoint URL | Falls back to `AZURE_AI_FOUNDRY_PROJECT_ENDPOINT`, `ConnectionStrings__foundryProject` (`Endpoint=` segment), then `FOUNDRY_ENDPOINT` |
| `AI_EVAL_MODEL_NAME` | ✅ | Runtime model deployment name | Falls back to `FOUNDRY_MODEL_NAME` |
| `AI_EVAL_JUDGE_MODEL_NAME` | ❌ | Judge model deployment name | Defaults to runtime model name |
| `AI_EVAL_API_BASE_URL` | ❌ | Deployed API base URL | Falls back to `AZURE_CONTAINER_APP_FQDN` and normalizes missing scheme to `https://` |
| `AI_EVAL_ACCESS_CODE` | ❌ | Access code for protected APIs | Falls back to `ACCESS_CODE` |
| `AI_EVAL_STORAGE_ROOT` | ❌ | Evaluation output directory | Defaults to `marginalia-service/TestResults/AiEvaluationStorage` |
| `AI_EVAL_EXECUTION_NAME` | ❌ | Reporting execution name | Defaults to `GITHUB_RUN_ID`, then `yyyyMMddTHHmmss` UTC timestamp |
| `AI_EVAL_USER_ID` | ❌ | User context for deployed API tests | Defaults to `ai-eval` |
| `AI_EVAL_ENABLE_CACHE` | ❌ | Enables judge response caching | Defaults to `false` |

## CI/CD Integration

The evaluation suite runs automatically in the `ai-evaluation` job of the e2e-test GitHub Actions workflow (`.github/workflows/e2e-test.yml`). The job:

1. Validates runtime and judge model deployments exist
1. Runs both in-process and deployed canary evaluations
1. Publishes test results via TRX format
1. Generates and uploads the HTML report
1. Uploads two artifacts:
   - `ai-evaluation-test-results-<environment>`
   - `ai-evaluation-report-<environment>`

### Artifacts

On CI completion, the following artifacts are available for download:

- Evaluation test results (TRX)
- Evaluation storage folder including `report.html`

### Example: View CI Artifacts

1. Go to the e2e-test workflow run in GitHub Actions
1. Scroll to **Artifacts** section
1. Download `ai-evaluation-report-<environment>`
1. Open `report.html`

## Troubleshooting

### "Unauthorized" or "403 Forbidden" from Foundry

Check that:

- `AI_EVAL_FOUNDRY_PROJECT_ENDPOINT` is correct and reachable
- Your Azure credentials are current (`az account get-access-token`)
- The runtime and judge model deployment names are valid

### "Connection refused" for API canary

If running deployed canary tests locally:

- Verify `AI_EVAL_API_BASE_URL` is reachable from your machine
- Check network and firewall rules
- Confirm `AI_EVAL_ACCESS_CODE` is correct (if required)

### Deployed canary test marked inconclusive

This occurs when `AI_EVAL_API_BASE_URL` and `AZURE_CONTAINER_APP_FQDN` are both missing.

Set either variable to run `AIEvalDeployed` scenarios.

### Missing Relevance or Coherence metric

The suite fails if required quality metrics are not produced. This typically indicates judge evaluator execution failure, response shape drift, or unexpected metric naming.

Inspect the reported diagnostics and available numeric metric names in the test failure output.

### HTML report not generated

Ensure `dotnet tool restore` ran before `dotnet aieval report`:

```powershell
dotnet tool restore
dotnet aieval report -p "$env:AI_EVAL_STORAGE_ROOT" -o "$env:AI_EVAL_STORAGE_ROOT\report.html"
```

If the command fails, verify the storage root path exists and contains evaluation results.

## Next Steps

- Expand scenario coverage in [marginalia-service/tests/evaluation/Ai.EvaluationTests/Scenarios/foundry-suggestion-scenarios.json](../../marginalia-service/tests/evaluation/Ai.EvaluationTests/Scenarios/foundry-suggestion-scenarios.json)
- Add or refine structural evaluators in [marginalia-service/tests/evaluation/Ai.EvaluationTests/SuggestionStructureEvaluators.cs](../../marginalia-service/tests/evaluation/Ai.EvaluationTests/SuggestionStructureEvaluators.cs)
- Add stricter policy gates in CI if you need explicit score thresholds instead of pass/fail interpretation checks
- Track evaluator behavior across model updates to detect regressions

## References

- [Microsoft.Extensions.AI.Evaluation on GitHub](https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.AI.Evaluation)
- [Testing Guide](./testing.md) — Overview of all Marginalia test suites
- [Local Development](../quickstart-local.md) — Setting up local Foundry credentials and model access
