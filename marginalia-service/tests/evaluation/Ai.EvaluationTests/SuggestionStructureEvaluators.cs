using System.Text.RegularExpressions;
using Marginalia.Domain.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace Marginalia.Ai.EvaluationTests;

internal sealed class SuggestionScenarioContext : EvaluationContext
{
    public SuggestionScenarioContext(FoundrySuggestionScenario scenario, IReadOnlyList<Suggestion> suggestions)
        : base("suggestion-scenario", BuildContextContents(scenario))
    {
        Scenario = scenario;
        Suggestions = suggestions;
    }

    public FoundrySuggestionScenario Scenario { get; }

    public IReadOnlyList<Suggestion> Suggestions { get; }

    private static string BuildContextContents(FoundrySuggestionScenario scenario) =>
        $"ScenarioId: {scenario.Id}\nDescription: {scenario.Description}\nExpectedTargets: {string.Join(", ", scenario.ExpectedTargetParagraphIds)}";
}

internal abstract class SuggestionScenarioEvaluatorBase : IEvaluator
{
    public abstract IReadOnlyCollection<string> EvaluationMetricNames { get; }

    public abstract ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default);

    protected static SuggestionScenarioContext GetContext(IEnumerable<EvaluationContext>? additionalContext) =>
        additionalContext?.OfType<SuggestionScenarioContext>().FirstOrDefault()
        ?? throw new InvalidOperationException("SuggestionScenarioContext is required for the structural evaluators.");

    protected static EvaluationResult BuildResult(string metricName, bool passed, string reason)
    {
        var metric = new NumericMetric(metricName, passed ? 5 : 1, reason)
        {
            Interpretation = new EvaluationMetricInterpretation(
                passed ? EvaluationRating.Good : EvaluationRating.Unacceptable,
                failed: !passed,
                reason: reason)
        };

        return new EvaluationResult(metric);
    }

    protected static string NormalizeWhitespace(string input) =>
        Regex.Replace(input, "\\s+", " ").Trim();
}

internal sealed class ParagraphMappingEvaluator : SuggestionScenarioEvaluatorBase
{
    public const string MetricName = "Valid paragraph mapping";

    public override IReadOnlyCollection<string> EvaluationMetricNames => [MetricName];

    public override ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        var context = GetContext(additionalContext);
        var validParagraphIds = context.Scenario.Paragraphs
            .Select(paragraph => paragraph.Id)
            .ToHashSet(StringComparer.Ordinal);

        var invalidTargets = context.Suggestions
            .Where(suggestion => !validParagraphIds.Contains(suggestion.ParagraphId))
            .Select(suggestion => suggestion.ParagraphId)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var passed = invalidTargets.Count == 0;
        var reason = passed
            ? "Every suggestion mapped to a valid paragraph in the evaluation scenario."
            : $"Suggestions referenced invalid paragraph IDs: {string.Join(", ", invalidTargets)}.";

        return new ValueTask<EvaluationResult>(BuildResult(MetricName, passed, reason));
    }
}

internal sealed class UniqueParagraphTargetEvaluator : SuggestionScenarioEvaluatorBase
{
    public const string MetricName = "One suggestion per paragraph";

    public override IReadOnlyCollection<string> EvaluationMetricNames => [MetricName];

    public override ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        var context = GetContext(additionalContext);
        var duplicateTargets = context.Suggestions
            .GroupBy(suggestion => suggestion.ParagraphId, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        var passed = duplicateTargets.Count == 0;
        var reason = passed
            ? "The scenario returned at most one suggestion for each paragraph."
            : $"Duplicate suggestions were returned for paragraphs: {string.Join(", ", duplicateTargets)}.";

        return new ValueTask<EvaluationResult>(BuildResult(MetricName, passed, reason));
    }
}

internal sealed class SuggestionFieldsEvaluator : SuggestionScenarioEvaluatorBase
{
    public const string MetricName = "Complete suggestion fields";

    public override IReadOnlyCollection<string> EvaluationMetricNames => [MetricName];

    public override ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        var context = GetContext(additionalContext);
        var missingFieldTargets = context.Suggestions
            .Where(suggestion =>
                string.IsNullOrWhiteSpace(suggestion.Rationale) ||
                string.IsNullOrWhiteSpace(suggestion.ProposedChange))
            .Select(suggestion => suggestion.ParagraphId)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var passed = context.Suggestions.Count > 0 && missingFieldTargets.Count == 0;
        var reason = passed
            ? "Every suggestion included both a rationale and a proposed replacement paragraph."
            : missingFieldTargets.Count > 0
                ? $"Suggestions for paragraphs {string.Join(", ", missingFieldTargets)} had empty rationale or proposedChange fields."
                : "The scenario returned no suggestions to evaluate.";

        return new ValueTask<EvaluationResult>(BuildResult(MetricName, passed, reason));
    }
}

internal sealed class ExpectedCoverageEvaluator : SuggestionScenarioEvaluatorBase
{
    public const string MetricName = "Expected target coverage";

    public override IReadOnlyCollection<string> EvaluationMetricNames => [MetricName];

    public override ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        var context = GetContext(additionalContext);
        var targetedParagraphIds = context.Suggestions
            .Select(suggestion => suggestion.ParagraphId)
            .ToHashSet(StringComparer.Ordinal);

        var missingExpectedTargets = context.Scenario.ExpectedTargetParagraphIds
            .Where(expectedTarget => !targetedParagraphIds.Contains(expectedTarget))
            .ToList();

        var unexpectedTargets = context.Scenario.AllowedTargetParagraphIds.Count == 0
            ? []
            : context.Suggestions
                .Where(suggestion => !context.Scenario.AllowedTargetParagraphIds.Contains(suggestion.ParagraphId, StringComparer.Ordinal))
                .Select(suggestion => suggestion.ParagraphId)
                .Distinct(StringComparer.Ordinal)
                .ToList();

        var countWithinRange = context.Suggestions.Count >= context.Scenario.MinimumSuggestionCount &&
                               (!context.Scenario.MaximumSuggestionCount.HasValue || context.Suggestions.Count <= context.Scenario.MaximumSuggestionCount.Value);

        var passed = missingExpectedTargets.Count == 0 &&
                     unexpectedTargets.Count == 0 &&
                     countWithinRange;

        var countReason = context.Scenario.MaximumSuggestionCount.HasValue
            ? $"Expected {context.Scenario.MinimumSuggestionCount} to {context.Scenario.MaximumSuggestionCount.Value} suggestions, received {context.Suggestions.Count}."
            : $"Expected at least {context.Scenario.MinimumSuggestionCount} suggestions, received {context.Suggestions.Count}.";

        var reason = passed
            ? $"{countReason} The expected target paragraphs were covered without unexpected targets."
            : $"{countReason} Missing expected targets: {FormatList(missingExpectedTargets)}. Unexpected targets: {FormatList(unexpectedTargets)}.";

        return new ValueTask<EvaluationResult>(BuildResult(MetricName, passed, reason));
    }

    private static string FormatList(IReadOnlyCollection<string> values) =>
        values.Count == 0 ? "(none)" : string.Join(", ", values);
}

internal sealed class MeaningfulRewriteEvaluator : SuggestionScenarioEvaluatorBase
{
    public const string MetricName = "Meaningful rewrite";

    public override IReadOnlyCollection<string> EvaluationMetricNames => [MetricName];

    public override ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        var context = GetContext(additionalContext);
        var paragraphLookup = context.Scenario.GetParagraphTextById();

        var unchangedTargets = context.Suggestions
            .Where(suggestion =>
                !paragraphLookup.TryGetValue(suggestion.ParagraphId, out var originalText) ||
                NormalizeWhitespace(originalText) == NormalizeWhitespace(suggestion.ProposedChange))
            .Select(suggestion => suggestion.ParagraphId)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var passed = context.Suggestions.Count > 0 && unchangedTargets.Count == 0;
        var reason = passed
            ? "Each suggestion proposed a materially different replacement paragraph."
            : unchangedTargets.Count > 0
                ? $"Suggestions for paragraphs {string.Join(", ", unchangedTargets)} did not materially differ from the source paragraph."
                : "The scenario returned no suggestions to evaluate.";

        return new ValueTask<EvaluationResult>(BuildResult(MetricName, passed, reason));
    }
}
