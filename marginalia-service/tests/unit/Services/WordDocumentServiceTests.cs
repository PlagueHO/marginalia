using DocumentFormat.OpenXml.Packaging;
using OpenXmlParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using FluentAssertions;
using Marginalia.Domain.Models;
using Marginalia.Infrastructure.Services;

namespace Marginalia.Tests.Unit.Services;

[TestClass]
[TestCategory("Unit")]
public sealed class WordDocumentServiceTests
{
    private static readonly WordDocumentService Service = new();

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Marginalia.Domain.Models.Document BuildDocument(
        IReadOnlyList<Marginalia.Domain.Models.Paragraph> paragraphs,
        params Suggestion[] suggestions) => new()
        {
            Id = "doc-1",
            Filename = "test.docx",
            Source = DocumentSource.Local,
            Paragraphs = paragraphs,
            Suggestions = suggestions.ToList().AsReadOnly()
        };

    private static IReadOnlyList<Marginalia.Domain.Models.Paragraph> MakeParagraphs(params string[] texts) =>
        texts.Select((t, i) => new Marginalia.Domain.Models.Paragraph { Id = $"p{i + 1}", Text = t })
            .ToList()
            .AsReadOnly();

    private static Suggestion BuildSuggestion(
        string paragraphId,
        string proposedChange,
        SuggestionStatus status = SuggestionStatus.Accepted,
        string? userSteeringInput = null) => new()
        {
            Id = Guid.NewGuid().ToString("N"),
            DocumentId = "doc-1",
            ParagraphId = paragraphId,
            Rationale = "test",
            ProposedChange = proposedChange,
            Status = status,
            UserSteeringInput = userSteeringInput
        };

    private static string ReadDocxText(Stream stream)
    {
        stream.Position = 0;
        using var wordDoc = WordprocessingDocument.Open(stream, false);
        var body = wordDoc.MainDocumentPart!.Document!.Body!;
        return string.Join("\n\n", body.Elements<OpenXmlParagraph>()
            .Select(p => p.InnerText)
            .Where(t => !string.IsNullOrWhiteSpace(t)));
    }

    // -----------------------------------------------------------------------
    // ExportAsync — accepted suggestions applied
    // -----------------------------------------------------------------------

    [TestMethod]
    public async Task ExportAsync_AppliesAcceptedSuggestion_ReplacesParagraphText()
    {
        var paragraphs = MakeParagraphs(
            "The sun rose over the hill.",
            "The birds began to sing.");
        const string replacement = "Dawn broke softly, painting the hillside gold.";
        var doc = BuildDocument(paragraphs, BuildSuggestion("p1", replacement));

        var stream = await Service.ExportAsync(doc);
        var result = ReadDocxText(stream);

        result.Should().Contain(replacement);
        result.Should().Contain("The birds began to sing.");
        result.Should().NotContain("The sun rose over the hill.");
    }

    [TestMethod]
    public async Task ExportAsync_DoesNotApplyRejectedSuggestions()
    {
        var paragraphs = MakeParagraphs("The quick brown fox jumps over the lazy dog.");
        var doc = BuildDocument(paragraphs,
            BuildSuggestion("p1", "slow", SuggestionStatus.Rejected));

        var stream = await Service.ExportAsync(doc);
        var result = ReadDocxText(stream);

        result.Should().Be("The quick brown fox jumps over the lazy dog.");
    }

    [TestMethod]
    public async Task ExportAsync_AppliesModifiedSuggestion_UsingUserSteeringInput()
    {
        var paragraphs = MakeParagraphs("The quick brown fox.", "The lazy dog slept.");
        const string userEdit = "A nimble tawny fox.";
        var doc = BuildDocument(paragraphs,
            BuildSuggestion("p1", "A fast brown fox.", SuggestionStatus.Modified, userEdit));

        var stream = await Service.ExportAsync(doc);
        var result = ReadDocxText(stream);

        result.Should().Contain(userEdit);
        result.Should().NotContain("The quick brown fox.");
    }

    [TestMethod]
    public async Task ExportAsync_AppliesModifiedSuggestion_FallsBackToProposedChange_WhenUserSteeringInputIsNull()
    {
        var paragraphs = MakeParagraphs("The quick brown fox.", "The lazy dog slept.");
        const string proposedChange = "A fast brown fox.";
        var doc = BuildDocument(paragraphs,
            BuildSuggestion("p1", proposedChange, SuggestionStatus.Modified, null));

        var stream = await Service.ExportAsync(doc);
        var result = ReadDocxText(stream);

        result.Should().Contain(proposedChange);
    }

    [TestMethod]
    public async Task ExportAsync_AppliesMultipleSuggestions_ToDifferentParagraphs()
    {
        var paragraphs = MakeParagraphs(
            "First sentence.",
            "Second sentence.",
            "Third sentence.");
        var s1 = BuildSuggestion("p1", "Opening line.");
        var s2 = BuildSuggestion("p2", "Middle line.");
        var doc = BuildDocument(paragraphs, s1, s2);

        var stream = await Service.ExportAsync(doc);
        var result = ReadDocxText(stream);

        result.Should().Contain("Opening line.");
        result.Should().Contain("Middle line.");
        result.Should().Contain("Third sentence.");
    }

    [TestMethod]
    public async Task ExportAsync_ProducesValidDocx()
    {
        var paragraphs = MakeParagraphs("Simple content.");
        var doc = BuildDocument(paragraphs, BuildSuggestion("p1", "Replaced content."));

        var stream = await Service.ExportAsync(doc);
        stream.Position = 0;

        var act = () => WordprocessingDocument.Open(stream, false);
        act.Should().NotThrow("the exported stream should be a valid .docx file");
    }

    [TestMethod]
    public async Task ExportAsync_NoSuggestions_ReturnsOriginalContent()
    {
        var paragraphs = MakeParagraphs("No suggestions here.", "Everything stays the same.");
        var doc = BuildDocument(paragraphs);

        var stream = await Service.ExportAsync(doc);
        var result = ReadDocxText(stream);

        result.Should().Contain("No suggestions here.");
        result.Should().Contain("Everything stays the same.");
    }
}
