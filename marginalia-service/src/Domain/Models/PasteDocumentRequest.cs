using System.Text.Json.Serialization;

namespace Marginalia.Domain.Models;

/// <summary>
/// Request payload for pasting raw text as a new document.
/// </summary>
public sealed record PasteDocumentRequest
{
    [JsonPropertyName("content")]
    public required string Content { get; init; }

    [JsonPropertyName("filename")]
    public string? Filename { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }
}
