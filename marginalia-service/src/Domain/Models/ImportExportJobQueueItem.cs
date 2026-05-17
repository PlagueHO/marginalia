using System.Text.Json.Serialization;

namespace Marginalia.Domain.Models;

public sealed record ImportExportJobQueueItem
{
    [JsonPropertyName("jobId")]
    public required string JobId { get; init; }

    [JsonPropertyName("userId")]
    public required string UserId { get; init; }

    [JsonPropertyName("jobType")]
    public ImportExportJobType JobType { get; init; }
}
