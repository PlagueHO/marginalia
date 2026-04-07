using System.Text.Json.Serialization;

namespace Marginalia.Domain.Models;

/// <summary>
/// Response DTO indicating whether the application requires an access code.
/// </summary>
public sealed record AccessControlStatusResponse
{
    [JsonPropertyName("accessCodeRequired")]
    public required bool AccessCodeRequired { get; init; }
}
