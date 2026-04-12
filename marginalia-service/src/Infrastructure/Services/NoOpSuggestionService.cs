using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;

namespace Marginalia.Infrastructure.Services;

/// <summary>
/// No-op implementation of <see cref="ISuggestionService"/> used when no AI backend is configured.
/// Returns an empty suggestion list for all requests.
/// </summary>
public sealed class NoOpSuggestionService : ISuggestionService
{
    public Task<IReadOnlyList<Suggestion>> AnalyzeAsync(
        string documentId,
        IReadOnlyList<Paragraph> paragraphs,
        string? userGuidance,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Suggestion>>([]);
    }

    public Task<IReadOnlyList<Suggestion>> AnalyzeParagraphAsync(
        string documentId,
        Paragraph targetParagraph,
        IReadOnlyList<Paragraph> contextParagraphs,
        string? userGuidance,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Suggestion>>([]);
    }
}
