using System.Text.Json.Serialization;

namespace Marginalia.Domain.Models;

/// <summary>
/// Response wrapper for document listing endpoint.
/// </summary>
public sealed record DocumentListResponse
{
    [JsonPropertyName("documents")]
    public required IReadOnlyList<DocumentSummary> Documents { get; init; }
}
