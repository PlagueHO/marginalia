using System.Text.Json.Serialization;
using Marginalia.Domain.Models;

namespace Marginalia.Ai.EvaluationTests;

internal sealed record FoundrySuggestionScenarioSet
{
    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("scenarios")]
    public required IReadOnlyList<FoundrySuggestionScenario> Scenarios { get; init; }
}

internal sealed record FoundrySuggestionScenario
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("userGuidance")]
    public string? UserGuidance { get; init; }

    [JsonPropertyName("paragraphs")]
    public required IReadOnlyList<Paragraph> Paragraphs { get; init; }

    [JsonPropertyName("expectedTargetParagraphIds")]
    public IReadOnlyList<string> ExpectedTargetParagraphIds { get; init; } = [];

    [JsonPropertyName("allowedTargetParagraphIds")]
    public IReadOnlyList<string> AllowedTargetParagraphIds { get; init; } = [];

    [JsonPropertyName("minimumSuggestionCount")]
    public int MinimumSuggestionCount { get; init; } = 1;

    [JsonPropertyName("maximumSuggestionCount")]
    public int? MaximumSuggestionCount { get; init; }

    [JsonPropertyName("runInDeployedCanary")]
    public bool RunInDeployedCanary { get; init; }

    public IReadOnlyDictionary<string, string> GetParagraphTextById() =>
        Paragraphs.ToDictionary(paragraph => paragraph.Id, paragraph => paragraph.Text, StringComparer.Ordinal);
}
