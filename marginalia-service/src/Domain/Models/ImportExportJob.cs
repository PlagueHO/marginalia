using System.Text.Json.Serialization;

namespace Marginalia.Domain.Models;

public sealed record ImportExportJob
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("userId")]
    public required string UserId { get; init; }

    [JsonPropertyName("jobType")]
    public ImportExportJobType JobType { get; init; }

    [JsonPropertyName("status")]
    public JobStatus Status { get; init; } = JobStatus.Queued;

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("startedAt")]
    public DateTimeOffset? StartedAt { get; init; }

    [JsonPropertyName("completedAt")]
    public DateTimeOffset? CompletedAt { get; init; }

    [JsonPropertyName("progressPercentage")]
    public int ProgressPercentage { get; init; }

    [JsonPropertyName("currentStage")]
    public string? CurrentStage { get; init; }

    [JsonPropertyName("totalItems")]
    public int TotalItems { get; init; }

    [JsonPropertyName("processedItems")]
    public int ProcessedItems { get; init; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; init; }

    [JsonPropertyName("overwriteExisting")]
    public bool OverwriteExisting { get; init; }

    [JsonPropertyName("isMultiUserMode")]
    public bool IsMultiUserMode { get; init; }

    [JsonPropertyName("resultFilePath")]
    public string? ResultFilePath { get; init; }

    [JsonPropertyName("sourceFilePath")]
    public string? SourceFilePath { get; init; }

    [JsonPropertyName("counts")]
    public ImportExportCounts? Counts { get; init; }
}
