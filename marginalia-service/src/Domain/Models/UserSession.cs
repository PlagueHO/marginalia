using System.Text.Json.Serialization;

namespace Marginalia.Domain.Models;

/// <summary>
/// Tracks an author's editing session with one or more documents.
/// </summary>
public sealed record UserSession
{
    [JsonPropertyName("id")]
    public string Id => SessionId;

    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    [JsonPropertyName("userId")]
    public string UserId { get; init; } = "_anonymous";

    [JsonPropertyName("documentIds")]
    public required IReadOnlyList<string> DocumentIds { get; init; }

    [JsonPropertyName("timestamp")]
    public required DateTimeOffset Timestamp { get; init; }
}
