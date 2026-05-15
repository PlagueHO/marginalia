using System.Text.Json.Serialization;

namespace Marginalia.Domain.Models;

/// <summary>
/// Response returned after importing manuscripts from a ZIP archive.
/// </summary>
public sealed record ImportDocumentsResponse
{
    [JsonPropertyName("importedCount")]
    public required int ImportedCount { get; init; }
}
