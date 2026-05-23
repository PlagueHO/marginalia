using FluentAssertions;
using Microsoft.Extensions.AI.Evaluation;

namespace Marginalia.Ai.EvaluationTests;

[TestClass]
public sealed class EvaluationMetricLookupTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void FindNumericMetricReturnsExactMetricWhenNameMatches()
    {
        var relevanceMetric = new NumericMetric("Relevance", 4, "reason");
        var result = CreateResult(relevanceMetric);

        var metric = EvaluationMetricLookup.FindNumericMetric(result, "Relevance");

        metric.Should().BeSameAs(relevanceMetric);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void FindNumericMetricReturnsMetricWhenNameContainsRequestedMetric()
    {
        var relevanceMetric = new NumericMetric("Quality/Relevance", 4, "reason");
        var result = CreateResult(relevanceMetric);

        var metric = EvaluationMetricLookup.FindNumericMetric(result, "Relevance");

        metric.Should().BeSameAs(relevanceMetric);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void FindNumericMetricReturnsMetricWhenNameMatchesCaseInsensitive()
    {
        var relevanceMetric = new NumericMetric("Relevance", 4, "reason");
        var result = CreateResult(relevanceMetric);

        var metric = EvaluationMetricLookup.FindNumericMetric(result, "relevance");

        metric.Should().BeSameAs(relevanceMetric);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void FindNumericMetricReturnsNullWhenMetricMissing()
    {
        var result = CreateResult(new NumericMetric("Coherence", 4, "reason"));

        var metric = EvaluationMetricLookup.FindNumericMetric(result, "Relevance");

        metric.Should().BeNull();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void FormatNumericMetricDiagnosticsIncludesInterpretationAndReason()
    {
        var metric = new NumericMetric("Relevance", 4, "judge reason")
        {
            Interpretation = new EvaluationMetricInterpretation(
                EvaluationRating.Good,
                failed: false,
                reason: "accepted")
        };

        var diagnostics = EvaluationMetricLookup.FormatNumericMetricDiagnostics(CreateResult(metric));

        diagnostics.Should().Contain("Relevance");
        diagnostics.Should().Contain("rating=Good");
        diagnostics.Should().Contain("failed=False");
        diagnostics.Should().Contain("interpretation=accepted");
        diagnostics.Should().Contain("reason=judge reason");
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void FormatNumericMetricDiagnosticsReturnsNoneWhenResultHasNoNumericMetrics()
    {
        var diagnostics = EvaluationMetricLookup.FormatNumericMetricDiagnostics(new EvaluationResult());

        diagnostics.Should().Be("(none)");
    }

    private static EvaluationResult CreateResult(params NumericMetric[] metrics) =>
        new()
        {
            Metrics = metrics.ToDictionary(metric => metric.Name, metric => (EvaluationMetric)metric, StringComparer.Ordinal)
        };
}
