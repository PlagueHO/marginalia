using FluentAssertions;

namespace Marginalia.Ai.EvaluationTests;

[TestClass]
public sealed class FoundrySuggestionEvaluationEnvironmentTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void LoadUsesDedicatedJudgeModelWhenProvided()
    {
        using var scope = new EnvironmentVariableScope(
            ("AI_EVAL_FOUNDRY_PROJECT_ENDPOINT", "https://example.services.ai.azure.com/api/projects/marginalia"),
            ("AI_EVAL_MODEL_NAME", "reviewer"),
            ("AI_EVAL_JUDGE_MODEL_NAME", "judge"),
            ("AI_EVAL_STORAGE_ROOT", "TestResults\\AiEvaluationStorage"),
            ("AI_EVAL_EXECUTION_NAME", "eval-run"));

        var environment = FoundrySuggestionEvaluationEnvironment.Load();

        environment.ModelName.Should().Be("reviewer");
        environment.JudgeModelName.Should().Be("judge");
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void LoadFallsBackToRuntimeModelWhenJudgeModelMissing()
    {
        using var scope = new EnvironmentVariableScope(
            ("AI_EVAL_FOUNDRY_PROJECT_ENDPOINT", "https://example.services.ai.azure.com/api/projects/marginalia"),
            ("AI_EVAL_MODEL_NAME", "reviewer"),
            ("AI_EVAL_STORAGE_ROOT", "TestResults\\AiEvaluationStorage"),
            ("AI_EVAL_EXECUTION_NAME", "eval-run"));

        var environment = FoundrySuggestionEvaluationEnvironment.Load();

        environment.ModelName.Should().Be("reviewer");
        environment.JudgeModelName.Should().Be("reviewer");
    }

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private static readonly string[] s_relatedVariables =
        [
            "AI_EVAL_FOUNDRY_PROJECT_ENDPOINT",
            "AZURE_AI_FOUNDRY_PROJECT_ENDPOINT",
            "ConnectionStrings__foundryProject",
            "FOUNDRY_ENDPOINT",
            "AI_EVAL_MODEL_NAME",
            "FOUNDRY_MODEL_NAME",
            "AI_EVAL_JUDGE_MODEL_NAME",
            "AI_EVAL_API_BASE_URL",
            "AZURE_CONTAINER_APP_FQDN",
            "AI_EVAL_STORAGE_ROOT",
            "AI_EVAL_EXECUTION_NAME",
            "AI_EVAL_USER_ID",
            "AI_EVAL_ACCESS_CODE",
            "ACCESS_CODE"
        ];

        private readonly Dictionary<string, string?> _originalValues = s_relatedVariables.ToDictionary(
            name => name,
            Environment.GetEnvironmentVariable,
            StringComparer.Ordinal);

        public EnvironmentVariableScope(params (string Name, string? Value)[] values)
        {
            foreach (var variableName in s_relatedVariables)
            {
                Environment.SetEnvironmentVariable(variableName, null);
            }

            foreach (var (name, value) in values)
            {
                Environment.SetEnvironmentVariable(name, value);
            }
        }

        public void Dispose()
        {
            foreach (var (name, value) in _originalValues)
            {
                Environment.SetEnvironmentVariable(name, value);
            }
        }
    }
}
