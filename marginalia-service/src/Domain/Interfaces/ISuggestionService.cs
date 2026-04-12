using Marginalia.Domain.Models;

namespace Marginalia.Domain.Interfaces;

/// <summary>
/// Contract for the suggestion engine that analyzes text and produces editorial suggestions.
/// </summary>
public interface ISuggestionService
{
    /// <summary>
    /// Analyzes the given document paragraphs and returns AI-generated suggestions.
    /// </summary>
    Task<IReadOnlyList<Suggestion>> AnalyzeAsync(
        string documentId,
        IReadOnlyList<Paragraph> paragraphs,
        string? userGuidance,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes a single target paragraph with surrounding context paragraphs.
    /// </summary>
    Task<IReadOnlyList<Suggestion>> AnalyzeParagraphAsync(
        string documentId,
        Paragraph targetParagraph,
        IReadOnlyList<Paragraph> contextParagraphs,
        string? userGuidance,
        CancellationToken cancellationToken = default);
}
