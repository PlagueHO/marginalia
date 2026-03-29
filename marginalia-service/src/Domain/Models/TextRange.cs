using System.Text.Json.Serialization;

namespace Marginalia.Domain.Models;

/// <summary>
/// Represents a text range within a document.
/// </summary>
public sealed record TextRange
{
    [JsonPropertyName("start")]
    public required int Start { get; init; }

    [JsonPropertyName("end")]
    public required int End { get; init; }
}
