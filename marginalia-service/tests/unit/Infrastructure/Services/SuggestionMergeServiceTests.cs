using FluentAssertions;
using Marginalia.Domain.Models;
using Marginalia.Infrastructure.Services;

namespace Marginalia.Tests.Unit.Infrastructure.Services;

[TestClass]
[TestCategory("Unit")]
public sealed class SuggestionMergeServiceTests
{
    private readonly SuggestionMergeService _service = new();

    [TestMethod]
    public void ApplyAcceptedSuggestionsToParagraphs_WithEmptySuggestions_ReturnsOriginalParagraphs()
    {
        var paragraphs = new List<Paragraph>
        {
            new() { Id = "p1", Text = "The quick brown fox jumps over the lazy dog." }
        }.AsReadOnly();
        var suggestions = new List<Suggestion>().AsReadOnly();

        var result = _service.ApplyAcceptedSuggestionsToParagraphs(paragraphs, suggestions);

        result.Should().HaveCount(1);
        result[0].Text.Should().Be("The quick brown fox jumps over the lazy dog.");
    }

    [TestMethod]
    public void ApplyAcceptedSuggestionsToParagraphs_WithAcceptedSuggestion_ReplacesParagraphText()
    {
        var paragraphs = new List<Paragraph>
        {
            new() { Id = "p1", Text = "The quick brown fox." }
        }.AsReadOnly();
        var suggestions = new List<Suggestion>
        {
            new()
            {
                Id = "s1",
                DocumentId = "doc1",
                ParagraphId = "p1",
                Rationale = "Replace",
                ProposedChange = "A swift red fox.",
                Status = SuggestionStatus.Accepted
            }
        }.AsReadOnly();

        var result = _service.ApplyAcceptedSuggestionsToParagraphs(paragraphs, suggestions);

        result.Should().HaveCount(1);
        result[0].Text.Should().Be("A swift red fox.");
        result[0].Id.Should().Be("p1");
    }

    [TestMethod]
    public void ApplyAcceptedSuggestionsToParagraphs_WithModifiedSuggestion_UsesUserSteeringInput()
    {
        var paragraphs = new List<Paragraph>
        {
            new() { Id = "p1", Text = "Original text." }
        }.AsReadOnly();
        var suggestions = new List<Suggestion>
        {
            new()
            {
                Id = "s1",
                DocumentId = "doc1",
                ParagraphId = "p1",
                Rationale = "Improve",
                ProposedChange = "AI proposed change.",
                Status = SuggestionStatus.Modified,
                UserSteeringInput = "User's preferred version."
            }
        }.AsReadOnly();

        var result = _service.ApplyAcceptedSuggestionsToParagraphs(paragraphs, suggestions);

        result[0].Text.Should().Be("User's preferred version.");
    }

    [TestMethod]
    public void ApplyAcceptedSuggestionsToParagraphs_WithModifiedSuggestion_FallsBackToProposedChange_WhenNoUserInput()
    {
        var paragraphs = new List<Paragraph>
        {
            new() { Id = "p1", Text = "Original text." }
        }.AsReadOnly();
        var suggestions = new List<Suggestion>
        {
            new()
            {
                Id = "s1",
                DocumentId = "doc1",
                ParagraphId = "p1",
                Rationale = "Improve",
                ProposedChange = "AI proposed change.",
                Status = SuggestionStatus.Modified
            }
        }.AsReadOnly();

        var result = _service.ApplyAcceptedSuggestionsToParagraphs(paragraphs, suggestions);

        result[0].Text.Should().Be("AI proposed change.");
    }

    [TestMethod]
    public void ApplyAcceptedSuggestionsToParagraphs_IgnoresRejectedAndPendingSuggestions()
    {
        var paragraphs = new List<Paragraph>
        {
            new() { Id = "p1", Text = "Original text." }
        }.AsReadOnly();
        var suggestions = new List<Suggestion>
        {
            new()
            {
                Id = "s1",
                DocumentId = "doc1",
                ParagraphId = "p1",
                Rationale = "Rejected",
                ProposedChange = "Should not appear.",
                Status = SuggestionStatus.Rejected
            },
            new()
            {
                Id = "s2",
                DocumentId = "doc1",
                ParagraphId = "p1",
                Rationale = "Pending",
                ProposedChange = "Should not appear either.",
                Status = SuggestionStatus.Pending
            }
        }.AsReadOnly();

        var result = _service.ApplyAcceptedSuggestionsToParagraphs(paragraphs, suggestions);

        result[0].Text.Should().Be("Original text.");
    }

    [TestMethod]
    public void ApplyAcceptedSuggestionsToParagraphs_WithMultipleParagraphs_AppliesCorrectly()
    {
        var paragraphs = new List<Paragraph>
        {
            new() { Id = "p1", Text = "First paragraph." },
            new() { Id = "p2", Text = "Second paragraph." },
            new() { Id = "p3", Text = "Third paragraph." }
        }.AsReadOnly();
        var suggestions = new List<Suggestion>
        {
            new()
            {
                Id = "s1",
                DocumentId = "doc1",
                ParagraphId = "p2",
                Rationale = "Improve",
                ProposedChange = "Improved second paragraph.",
                Status = SuggestionStatus.Accepted
            }
        }.AsReadOnly();

        var result = _service.ApplyAcceptedSuggestionsToParagraphs(paragraphs, suggestions);

        result.Should().HaveCount(3);
        result[0].Text.Should().Be("First paragraph.");
        result[1].Text.Should().Be("Improved second paragraph.");
        result[2].Text.Should().Be("Third paragraph.");
    }

    [TestMethod]
    public void ApplyAcceptedSuggestionsToParagraphs_WithNonMatchingParagraphId_SkipsSuggestion()
    {
        var paragraphs = new List<Paragraph>
        {
            new() { Id = "p1", Text = "Original text." }
        }.AsReadOnly();
        var suggestions = new List<Suggestion>
        {
            new()
            {
                Id = "s1",
                DocumentId = "doc1",
                ParagraphId = "nonexistent",
                Rationale = "Should be skipped",
                ProposedChange = "Never applied.",
                Status = SuggestionStatus.Accepted
            }
        }.AsReadOnly();

        var result = _service.ApplyAcceptedSuggestionsToParagraphs(paragraphs, suggestions);

        result[0].Text.Should().Be("Original text.");
    }

    [TestMethod]
    public void ApplyAcceptedSuggestionsToParagraphs_PreservesParagraphIds()
    {
        var paragraphs = new List<Paragraph>
        {
            new() { Id = "p1", Text = "First." },
            new() { Id = "p2", Text = "Second." }
        }.AsReadOnly();
        var suggestions = new List<Suggestion>
        {
            new()
            {
                Id = "s1",
                DocumentId = "doc1",
                ParagraphId = "p1",
                Rationale = "Change",
                ProposedChange = "Updated first.",
                Status = SuggestionStatus.Accepted
            }
        }.AsReadOnly();

        var result = _service.ApplyAcceptedSuggestionsToParagraphs(paragraphs, suggestions);

        result[0].Id.Should().Be("p1");
        result[1].Id.Should().Be("p2");
    }
}
