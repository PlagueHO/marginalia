using Microsoft.Extensions.AI.Evaluation;

namespace Marginalia.Ai.EvaluationTests;

internal static class EvaluationMetricLookup
{
    internal static NumericMetric? FindNumericMetric(EvaluationResult result, string metricName)
    {
        if (result.TryGet(metricName, out NumericMetric? metric) && metric is not null)
        {
            return metric;
        }

        var numericMetrics = result.Metrics.Values.OfType<NumericMetric>().ToList();

        return numericMetrics.FirstOrDefault(item => string.Equals(item.Name, metricName, StringComparison.OrdinalIgnoreCase))
            ?? numericMetrics
                .Where(item => item.Name.Contains(metricName, StringComparison.OrdinalIgnoreCase))
                // Prefer the shortest containing match to avoid selecting broad prefixed/suffixed variants when multiple candidates exist.
                .OrderBy(item => item.Name.Length)
                .FirstOrDefault();
    }

    internal static string FormatNumericMetricDiagnostics(EvaluationResult result)
    {
        var diagnostics = result.Metrics.Values
            .OfType<NumericMetric>()
            .OrderBy(metric => metric.Name, StringComparer.Ordinal)
            .Select(FormatMetricDiagnostic)
            .ToList();

        return diagnostics.Count == 0 ? "(none)" : string.Join("; ", diagnostics);
    }

    internal static string FormatAvailableMetricNames(EvaluationResult result)
    {
        var metricNames = result.Metrics.Values
            .OfType<NumericMetric>()
            .Select(metric => metric.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        return metricNames.Count == 0 ? "(none)" : string.Join(", ", metricNames);
    }

    private static string FormatMetricDiagnostic(NumericMetric metric)
    {
        List<string> details = [];

        if (metric.Interpretation is not null)
        {
            details.Add($"rating={metric.Interpretation.Rating}");
            details.Add($"failed={metric.Interpretation.Failed}");

            if (!string.IsNullOrWhiteSpace(metric.Interpretation.Reason))
            {
                details.Add($"interpretation={metric.Interpretation.Reason}");
            }
        }

        if (!string.IsNullOrWhiteSpace(metric.Reason) &&
            !string.Equals(metric.Reason, metric.Interpretation?.Reason, StringComparison.Ordinal))
        {
            details.Add($"reason={metric.Reason}");
        }

        return details.Count == 0
            ? metric.Name
            : $"{metric.Name} [{string.Join(", ", details)}]";
    }
}
