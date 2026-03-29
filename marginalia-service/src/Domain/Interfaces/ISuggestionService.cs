using Marginalia.Domain.Models;

namespace Marginalia.Domain.Interfaces;

/// <summary>
/// Contract for the suggestion engine that analyzes text and produces editorial suggestions.
/// </summary>
public interface ISuggestionService
{
    /// <summary>
    /// Analyzes the given document content and returns AI-generated suggestions.
    /// </summary>
    Task<IReadOnlyList<Suggestion>> AnalyzeAsync(
        string documentId,
        string content,
        string? userGuidance,
        CancellationToken cancellationToken = default);
}
