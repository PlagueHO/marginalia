using Marginalia.Domain.Models;

namespace Marginalia.Infrastructure.Services;

/// <summary>
/// Service for merging accepted suggestions into document paragraphs.
/// </summary>
public sealed class SuggestionMergeService
{
    /// <summary>
    /// Applies accepted suggestions to document paragraphs by replacing paragraph text
    /// with the proposed change. Only one accepted suggestion per paragraph is applied
    /// (the first found). Returns a new paragraph list with merged text.
    /// </summary>
    public IReadOnlyList<Paragraph> ApplyAcceptedSuggestionsToParagraphs(
        IReadOnlyList<Paragraph> paragraphs,
        IReadOnlyList<Suggestion> acceptedSuggestions)
    {
        if (acceptedSuggestions.Count == 0)
        {
            return paragraphs;
        }

        var suggestionsByParagraph = acceptedSuggestions
            .Where(s => s.Status is SuggestionStatus.Accepted or SuggestionStatus.Modified)
            .GroupBy(s => s.ParagraphId)
            .ToDictionary(g => g.Key, g => g.First());

        return paragraphs.Select(p =>
        {
            if (suggestionsByParagraph.TryGetValue(p.Id, out var suggestion))
            {
                var replacementText = suggestion.Status == SuggestionStatus.Modified
                    ? (suggestion.UserSteeringInput ?? suggestion.ProposedChange)
                    : suggestion.ProposedChange;

                return p with { Text = replacementText };
            }

            return p;
        }).ToList().AsReadOnly();
    }
}
