using System.Text.Json.Serialization;

namespace Marginalia.Domain.Models;

public sealed record ImportExportCounts
{
    [JsonPropertyName("documentsImported")]
    public int DocumentsImported { get; init; }

    [JsonPropertyName("documentsSkipped")]
    public int DocumentsSkipped { get; init; }

    [JsonPropertyName("failed")]
    public int Failed { get; init; }
}
