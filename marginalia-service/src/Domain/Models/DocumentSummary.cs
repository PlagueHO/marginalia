using System.Text.Json.Serialization;

namespace Marginalia.Domain.Models;

/// <summary>
/// Lightweight document listing DTO — no content or suggestions array.
/// </summary>
public sealed record DocumentSummary
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("filename")]
    public required string Filename { get; init; }

    [JsonPropertyName("source")]
    public required DocumentSource Source { get; init; }

    [JsonPropertyName("status")]
    public required DocumentStatus Status { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public required DateTimeOffset UpdatedAt { get; init; }

    [JsonPropertyName("suggestionCount")]
    public required int SuggestionCount { get; init; }

    [JsonPropertyName("paragraphCount")]
    public required int ParagraphCount { get; init; }
}
