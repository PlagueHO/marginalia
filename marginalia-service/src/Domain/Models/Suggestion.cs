using System.Text.Json.Serialization;

namespace Marginalia.Domain.Models;

/// <summary>
/// An AI-generated editorial suggestion targeting a specific paragraph within a document.
/// </summary>
public sealed record Suggestion
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("userId")]
    public string UserId { get; init; } = "_anonymous";

    [JsonPropertyName("documentId")]
    public required string DocumentId { get; init; }

    [JsonPropertyName("paragraphId")]
    public string ParagraphId { get; init; } = string.Empty;

    [JsonPropertyName("rationale")]
    public required string Rationale { get; init; }

    [JsonPropertyName("proposedChange")]
    public required string ProposedChange { get; init; }

    [JsonPropertyName("status")]
    public required SuggestionStatus Status { get; init; }

    [JsonPropertyName("userSteeringInput")]
    public string? UserSteeringInput { get; init; }
}
