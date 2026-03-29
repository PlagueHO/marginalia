using System.Text.Json.Serialization;

namespace Marginalia.Domain.Models;

/// <summary>
/// Response DTO for LLM endpoint configuration. Authentication is always Entra ID via Aspire.
/// </summary>
public sealed record LlmConfigResponse
{
    [JsonPropertyName("endpoint")]
    public string? Endpoint { get; init; }

    [JsonPropertyName("modelName")]
    public string? ModelName { get; init; }

    [JsonPropertyName("isConfigured")]
    public required bool IsConfigured { get; init; }

    [JsonPropertyName("authMethod")]
    public string? AuthMethod { get; init; }
}
