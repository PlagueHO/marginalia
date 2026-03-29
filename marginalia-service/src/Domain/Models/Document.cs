using System.Text.Json.Serialization;

namespace Marginalia.Domain.Models;

/// <summary>
/// A manuscript or document uploaded by the author for editorial analysis.
/// </summary>
public sealed record Document
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("userId")]
    public string UserId { get; init; } = "_anonymous";

    [JsonPropertyName("filename")]
    public required string Filename { get; init; }

    [JsonPropertyName("source")]
    public required DocumentSource Source { get; init; }

    [JsonPropertyName("content")]
    public required string Content { get; init; }

    [JsonPropertyName("suggestions")]
    public IReadOnlyList<Suggestion> Suggestions { get; init; } = [];
}
