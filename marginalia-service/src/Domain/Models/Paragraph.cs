using System.Text.Json.Serialization;

namespace Marginalia.Domain.Models;

/// <summary>
/// A single paragraph within a document, identified by a stable GUID.
/// </summary>
public sealed record Paragraph
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("text")]
    public required string Text { get; init; }
}
