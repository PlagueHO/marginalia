using System.Text.Json.Serialization;

namespace Marginalia.Domain.Models;

/// <summary>
/// An AI-generated editorial suggestion for a specific text range within a document.
/// </summary>
public sealed record Suggestion
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("userId")]
    public string UserId { get; init; } = "_anonymous";

    [JsonPropertyName("documentId")]
    public required string DocumentId { get; init; }

    [JsonPropertyName("textRange")]
    public required TextRange TextRange { get; init; }

    [JsonPropertyName("rationale")]
    public required string Rationale { get; init; }

    [JsonPropertyName("proposedChange")]
    public required string ProposedChange { get; init; }

    [JsonPropertyName("status")]
    public required SuggestionStatus Status { get; init; }

    [JsonPropertyName("userSteeringInput")]
    public string? UserSteeringInput { get; init; }
}
