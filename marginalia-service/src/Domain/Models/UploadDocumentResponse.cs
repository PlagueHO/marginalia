using System.Text.Json.Serialization;

namespace Marginalia.Domain.Models;

/// <summary>
/// Response returned from document upload and paste endpoints.
/// </summary>
public sealed record UploadDocumentResponse
{
    [JsonPropertyName("document")]
    public required Document Document { get; init; }

    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }
}
