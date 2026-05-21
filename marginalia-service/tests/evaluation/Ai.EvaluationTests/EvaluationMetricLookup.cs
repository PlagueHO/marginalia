using Microsoft.Extensions.AI.Evaluation;

namespace Marginalia.Ai.EvaluationTests;

internal static class EvaluationMetricLookup
{
    public static NumericMetric? FindNumericMetric(EvaluationResult result, string metricName)
    {
        if (result.TryGet(metricName, out NumericMetric? metric) && metric is not null)
        {
            return metric;
        }

        var numericMetrics = result.Metrics.Values.OfType<NumericMetric>().ToList();

        return numericMetrics.FirstOrDefault(item => string.Equals(item.Name, metricName, StringComparison.OrdinalIgnoreCase))
            ?? numericMetrics
                .Where(item => item.Name.Contains(metricName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.Name.Length)
                .FirstOrDefault();
    }

    public static string FormatAvailableMetricNames(EvaluationResult result)
    {
        var metricNames = result.Metrics.Values
            .OfType<NumericMetric>()
            .Select(metric => metric.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        return metricNames.Count == 0 ? "(none)" : string.Join(", ", metricNames);
    }
}
