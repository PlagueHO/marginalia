using System.Text.Json.Serialization;

namespace Marginalia.Domain.Models;

/// <summary>
/// Request payload for triggering AI analysis on a document.
/// </summary>
public sealed record AnalysisRequest
{
    [JsonPropertyName("documentId")]
    public required string DocumentId { get; init; }

    [JsonPropertyName("chunkIndex")]
    public int? ChunkIndex { get; init; }

    [JsonPropertyName("userInstructions")]
    public string? UserInstructions { get; init; }

    [JsonPropertyName("toneGuidance")]
    public string? ToneGuidance { get; init; }
}
