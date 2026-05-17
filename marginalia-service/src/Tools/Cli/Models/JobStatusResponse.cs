using System.Text.Json.Serialization;

namespace Marginalia.Tools.Cli.Models;

internal sealed record JobStatusResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("progressPercentage")]
    public int ProgressPercentage { get; init; }

    [JsonPropertyName("currentStage")]
    public string? CurrentStage { get; init; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; init; }
}
