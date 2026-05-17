using System.Text.Json.Serialization;
using Marginalia.Domain.Models;

namespace Marginalia.Api.Models;

public sealed record ImportExportJobResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("jobType")]
    public required ImportExportJobType JobType { get; init; }

    [JsonPropertyName("status")]
    public required JobStatus Status { get; init; }

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

    [JsonPropertyName("counts")]
    public ImportExportCounts? Counts { get; init; }

    public static ImportExportJobResponse FromDomain(ImportExportJob job)
    {
        return new ImportExportJobResponse
        {
            Id = job.Id,
            JobType = job.JobType,
            Status = job.Status,
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            ProgressPercentage = job.ProgressPercentage,
            CurrentStage = job.CurrentStage,
            TotalItems = job.TotalItems,
            ProcessedItems = job.ProcessedItems,
            ErrorMessage = job.ErrorMessage,
            OverwriteExisting = job.OverwriteExisting,
            Counts = job.Counts
        };
    }
}
