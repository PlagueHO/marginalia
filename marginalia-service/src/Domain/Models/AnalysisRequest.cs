using System.Text.Json.Serialization;

namespace Marginalia.Domain.Models;

/// <summary>
/// Request payload for triggering AI analysis on a document.
/// </summary>
public sealed record AnalysisRequest
{
    [JsonPropertyName("documentId")]
    public string? DocumentId { get; init; }

    [JsonPropertyName("userInstructions")]
    public string? UserInstructions { get; init; }

    [JsonPropertyName("toneGuidance")]
    public string? ToneGuidance { get; init; }

    // Backward-compatible aliases used by older frontend payloads.
    [JsonPropertyName("userGuidance")]
    public string? UserGuidance { get; init; }

    [JsonPropertyName("tone")]
    public string? Tone { get; init; }

    [JsonIgnore]
    public string? EffectiveUserInstructions =>
        !string.IsNullOrWhiteSpace(UserInstructions) ? UserInstructions : UserGuidance;

    [JsonIgnore]
    public string? EffectiveToneGuidance =>
        !string.IsNullOrWhiteSpace(ToneGuidance) ? ToneGuidance : Tone;
}
