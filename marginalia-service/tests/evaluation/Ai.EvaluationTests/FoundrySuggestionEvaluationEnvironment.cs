namespace Marginalia.Ai.EvaluationTests;

internal sealed record FoundrySuggestionEvaluationEnvironment
{
    public required Uri FoundryProjectEndpoint { get; init; }

    public required string ModelName { get; init; }

    public required string JudgeModelName { get; init; }

    public Uri? ApiBaseUrl { get; init; }

    public string? AccessCode { get; init; }

    public string UserId { get; init; } = "ai-eval";

    public required string StorageRootPath { get; init; }

    public required string ExecutionName { get; init; }

    public bool EnableResponseCaching { get; init; }

    public static FoundrySuggestionEvaluationEnvironment Load()
    {
        var projectEndpoint = FirstNonEmpty(
            Environment.GetEnvironmentVariable("AI_EVAL_FOUNDRY_PROJECT_ENDPOINT"),
            Environment.GetEnvironmentVariable("AZURE_AI_FOUNDRY_PROJECT_ENDPOINT"),
            ParseEndpointFromConnectionString(Environment.GetEnvironmentVariable("ConnectionStrings__foundryProject")),
            Environment.GetEnvironmentVariable("FOUNDRY_ENDPOINT"));

        if (string.IsNullOrWhiteSpace(projectEndpoint) ||
            !Uri.TryCreate(projectEndpoint, UriKind.Absolute, out var foundryProjectEndpoint))
        {
            throw new InvalidOperationException(
                "A Foundry project endpoint is required. Set AI_EVAL_FOUNDRY_PROJECT_ENDPOINT, AZURE_AI_FOUNDRY_PROJECT_ENDPOINT, ConnectionStrings__foundryProject, or FOUNDRY_ENDPOINT.");
        }

        var modelName = FirstNonEmpty(
            Environment.GetEnvironmentVariable("AI_EVAL_MODEL_NAME"),
            Environment.GetEnvironmentVariable("FOUNDRY_MODEL_NAME"));

        if (string.IsNullOrWhiteSpace(modelName))
        {
            throw new InvalidOperationException(
                "A model deployment name is required. Set AI_EVAL_MODEL_NAME or FOUNDRY_MODEL_NAME.");
        }

        var apiBaseUrl = FirstNonEmpty(
            Environment.GetEnvironmentVariable("AI_EVAL_API_BASE_URL"),
            Environment.GetEnvironmentVariable("AZURE_CONTAINER_APP_FQDN"));

        var storageRootPath = FirstNonEmpty(
            Environment.GetEnvironmentVariable("AI_EVAL_STORAGE_ROOT"),
            Path.Combine("marginalia-service", "TestResults", "AiEvaluationStorage"),
            Path.Combine("TestResults", "AiEvaluationStorage"))
            ?? throw new InvalidOperationException("Unable to resolve the AI evaluation storage root path.");

        var executionName = FirstNonEmpty(
            Environment.GetEnvironmentVariable("AI_EVAL_EXECUTION_NAME"),
            Environment.GetEnvironmentVariable("GITHUB_RUN_ID"),
            DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmss"))
            ?? throw new InvalidOperationException("Unable to resolve the AI evaluation execution name.");

        var userId = FirstNonEmpty(Environment.GetEnvironmentVariable("AI_EVAL_USER_ID"), "ai-eval")
                     ?? throw new InvalidOperationException("Unable to resolve the AI evaluation user ID.");

        return new FoundrySuggestionEvaluationEnvironment
        {
            FoundryProjectEndpoint = foundryProjectEndpoint,
            ModelName = modelName,
            JudgeModelName = FirstNonEmpty(Environment.GetEnvironmentVariable("AI_EVAL_JUDGE_MODEL_NAME"), modelName)
                             ?? throw new InvalidOperationException("Unable to resolve the AI evaluation judge model name."),
            ApiBaseUrl = string.IsNullOrWhiteSpace(apiBaseUrl) ? null : NormalizeApiBaseUrl(apiBaseUrl),
            AccessCode = FirstNonEmpty(
                Environment.GetEnvironmentVariable("AI_EVAL_ACCESS_CODE"),
                Environment.GetEnvironmentVariable("ACCESS_CODE")),
            UserId = userId,
            StorageRootPath = Path.GetFullPath(storageRootPath),
            ExecutionName = executionName,
            EnableResponseCaching = bool.TryParse(Environment.GetEnvironmentVariable("AI_EVAL_ENABLE_CACHE"), out var enableCache) && enableCache,
        };
    }

    private static string? ParseEndpointFromConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        foreach (var segment in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (segment.StartsWith("Endpoint=", StringComparison.OrdinalIgnoreCase))
            {
                return segment["Endpoint=".Length..];
            }
        }

        return null;
    }

    private static Uri NormalizeApiBaseUrl(string apiBaseUrl)
    {
        var candidate = apiBaseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                        apiBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? apiBaseUrl
            : $"https://{apiBaseUrl}";

        return new Uri(candidate, UriKind.Absolute);
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}
