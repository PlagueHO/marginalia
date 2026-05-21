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
    public void FindNumericMetricReturnsNullWhenMetricMissing()
    {
        var result = CreateResult(new NumericMetric("Coherence", 4, "reason"));

        var metric = EvaluationMetricLookup.FindNumericMetric(result, "Relevance");

        metric.Should().BeNull();
    }

    private static EvaluationResult CreateResult(params NumericMetric[] metrics) =>
        new()
        {
            Metrics = metrics.ToDictionary(metric => metric.Name, metric => (EvaluationMetric)metric, StringComparer.Ordinal)
        };
}
