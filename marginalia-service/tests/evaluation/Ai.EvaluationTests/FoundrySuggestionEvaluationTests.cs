using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;

namespace Marginalia.Ai.EvaluationTests;

[TestClass]
public sealed class FoundrySuggestionEvaluationTests
{
    private static FoundrySuggestionEvaluationEnvironment s_environment = null!;
    private static FoundrySuggestionScenarioSet s_scenarioSet = null!;
    private static ReportingConfiguration s_reportingConfiguration = null!;

    public TestContext? TestContext { get; set; }

    [ClassInitialize]
    public static void Initialize(TestContext _)
    {
        s_environment = FoundrySuggestionEvaluationEnvironment.Load();
        s_scenarioSet = FoundrySuggestionEvaluationSupport.LoadScenarioSet();
        s_reportingConfiguration = FoundrySuggestionEvaluationSupport.CreateReportingConfiguration(s_environment);
    }

    private string ScenarioName => $"{TestContext!.FullyQualifiedTestClassName}.{TestContext.TestName}";

    [TestMethod]
    [TestCategory("AIEval")]
    [TestCategory("AIEvalInProcess")]
    public async Task InProcessSuggestionScenariosMeetQualityBar()
    {
        var service = FoundrySuggestionEvaluationSupport.CreateSuggestionService(s_environment);

        foreach (var scenario in s_scenarioSet.Scenarios)
        {
            await using ScenarioRun scenarioRun =
                await s_reportingConfiguration.CreateScenarioRunAsync(
                    $"{ScenarioName}.{scenario.Id}",
                    additionalTags:
                    [
                        "in-process",
                        s_scenarioSet.Version,
                        $"sut:{s_environment.ModelName}",
                        $"judge:{s_environment.JudgeModelName}"
                    ]);

            var suggestions = await service.AnalyzeAsync(
                $"eval-{scenario.Id}",
                scenario.Paragraphs,
                scenario.UserGuidance);

            var result = await scenarioRun.EvaluateAsync(
                FoundrySuggestionEvaluationSupport.BuildEvaluationMessages(scenario),
                FoundrySuggestionEvaluationSupport.BuildEvaluationResponse(scenario, suggestions),
                additionalContext:
                [
                    new SuggestionScenarioContext(scenario, suggestions)
                ]);

            Validate(result, scenario.Id);
        }
    }

    [TestMethod]
    [TestCategory("AIEval")]
    [TestCategory("AIEvalDeployed")]
    public async Task DeployedApiCanaryScenariosMeetQualityBar()
    {
        if (s_environment.ApiBaseUrl is null)
        {
            Assert.Inconclusive("Set AI_EVAL_API_BASE_URL or AZURE_CONTAINER_APP_FQDN to run the deployed AI evaluation canary.");
        }

        foreach (var scenario in s_scenarioSet.Scenarios.Where(item => item.RunInDeployedCanary))
        {
            await using ScenarioRun scenarioRun =
                await s_reportingConfiguration.CreateScenarioRunAsync(
                    $"{ScenarioName}.{scenario.Id}",
                    additionalTags:
                    [
                        "deployed",
                        s_scenarioSet.Version,
                        $"sut:{s_environment.ModelName}",
                        $"judge:{s_environment.JudgeModelName}"
                    ]);

            var suggestions = await FoundrySuggestionEvaluationSupport.RunDeployedScenarioAsync(
                s_environment,
                scenario,
                CancellationToken.None);

            var result = await scenarioRun.EvaluateAsync(
                FoundrySuggestionEvaluationSupport.BuildEvaluationMessages(scenario),
                FoundrySuggestionEvaluationSupport.BuildEvaluationResponse(scenario, suggestions),
                additionalContext:
                [
                    new SuggestionScenarioContext(scenario, suggestions)
                ]);

            Validate(result, scenario.Id);
        }
    }

    private static void Validate(EvaluationResult result, string scenarioId)
    {
        using var scope = new AssertionScope();

        AssertPassed(result.Get<NumericMetric>(ParagraphMappingEvaluator.MetricName), scenarioId);
        AssertPassed(result.Get<NumericMetric>(UniqueParagraphTargetEvaluator.MetricName), scenarioId);
        AssertPassed(result.Get<NumericMetric>(SuggestionFieldsEvaluator.MetricName), scenarioId);
        AssertPassed(result.Get<NumericMetric>(ExpectedCoverageEvaluator.MetricName), scenarioId);
        AssertPassed(result.Get<NumericMetric>(MeaningfulRewriteEvaluator.MetricName), scenarioId);
        AssertPassedQualityMetric(result, RelevanceEvaluator.RelevanceMetricName, scenarioId);
        AssertPassedQualityMetric(result, CoherenceEvaluator.CoherenceMetricName, scenarioId);
    }

    private static void AssertPassed(NumericMetric metric, string scenarioId)
    {
        metric.Interpretation.Should().NotBeNull($"scenario '{scenarioId}' should return an interpretation for metric '{metric.Name}'.");
        metric.Interpretation!.Failed.Should().BeFalse(
            $"scenario '{scenarioId}' failed metric '{metric.Name}' with reason: {metric.Reason ?? metric.Interpretation.Reason}");
    }

    private static void AssertPassedQualityMetric(EvaluationResult result, string metricName, string scenarioId)
    {
        var metric = EvaluationMetricLookup.FindNumericMetric(result, metricName)
            ?? throw new AssertFailedException(
                $"scenario '{scenarioId}' should produce quality metric '{metricName}'. Available numeric metrics: {EvaluationMetricLookup.FormatAvailableMetricNames(result)}");

        AssertPassed(metric, scenarioId);
    }
}
