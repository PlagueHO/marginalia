using System.Text.Json.Serialization;

namespace Marginalia.Domain.Models;

public sealed record LlmHealthResponse
{
    [JsonPropertyName("healthy")]
    public required bool Healthy { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }
}
