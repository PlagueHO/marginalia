using System.Text.Json.Serialization;

namespace Marginalia.Domain.Models;

/// <summary>
/// A manuscript or document uploaded by the author for editorial analysis.
/// </summary>
public sealed record Document
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("userId")]
    public string UserId { get; init; } = "_anonymous";

    [JsonPropertyName("filename")]
    public required string Filename { get; init; }

    [JsonPropertyName("source")]
    public required DocumentSource Source { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = "";

    [JsonPropertyName("status")]
    public DocumentStatus Status { get; init; } = DocumentStatus.Draft;

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.MinValue;

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.MinValue;

    [JsonPropertyName("paragraphs")]
    public IReadOnlyList<Paragraph> Paragraphs { get; init; } = [];

    [JsonPropertyName("suggestions")]
    public IReadOnlyList<Suggestion> Suggestions { get; init; } = [];

    /// <summary>
    /// Joins all paragraph texts with double newlines to produce the full document text.
    /// </summary>
    [JsonIgnore]
    public string FullText => string.Join("\n\n", Paragraphs.Select(p => p.Text));

    /// <summary>
    /// Returns the 0-based index of the paragraph with the given ID.
    /// </summary>
    public int GetParagraphIndex(string paragraphId)
    {
        for (var i = 0; i < Paragraphs.Count; i++)
        {
            if (Paragraphs[i].Id == paragraphId)
            {
                return i;
            }
        }

        throw new ArgumentException($"Paragraph '{paragraphId}' not found in document.", nameof(paragraphId));
    }
}
