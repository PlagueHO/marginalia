using System.Text.Json.Serialization;

namespace Marginalia.Domain.Models;

/// <summary>
/// Request payload for updating a suggestion's status.
/// </summary>
public sealed record UpdateSuggestionRequest
{
    [JsonPropertyName("status")]
    public required SuggestionStatus Status { get; init; }

    [JsonPropertyName("userSteeringInput")]
    public string? UserSteeringInput { get; init; }
}
