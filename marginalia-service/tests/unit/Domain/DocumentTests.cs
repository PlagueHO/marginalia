using System.Text.Json;
using FluentAssertions;
using Marginalia.Domain.Models;

namespace Marginalia.Tests.Unit.Domain;

[TestClass]
[TestCategory("Unit")]
public sealed class DocumentTests
{
    [TestMethod]
    public void Constructor_WithRequiredFields_CreatesDocument()
    {
        var doc = new Document
        {
            Id = "doc-1",
            Filename = "chapter1.docx",
            Source = DocumentSource.Local,
            Paragraphs = [new Paragraph { Id = "p1", Text = "Once upon a time..." }]
        };

        doc.Id.Should().Be("doc-1");
        doc.Filename.Should().Be("chapter1.docx");
        doc.Source.Should().Be(DocumentSource.Local);
        doc.FullText.Should().Be("Once upon a time...");
    }

    [TestMethod]
    public void Suggestions_DefaultsToEmptyList_WhenNotProvided()
    {
        var doc = new Document
        {
            Id = "doc-1",
            Filename = "test.docx",
            Source = DocumentSource.Local,
            Paragraphs = [new Paragraph { Id = "p1", Text = "content" }]
        };

        doc.Suggestions.Should().BeEmpty();
    }

    [TestMethod]
    public void Suggestions_WhenProvided_AreAccessible()
    {
        var suggestion = new Suggestion
        {
            Id = "sug-1",
            DocumentId = "doc-1",
            ParagraphId = "p1",
            Rationale = "Too compressed",
            ProposedChange = "Expanded text",
            Status = SuggestionStatus.Pending
        };

        var doc = new Document
        {
            Id = "doc-1",
            Filename = "test.docx",
            Source = DocumentSource.Local,
            Paragraphs = [new Paragraph { Id = "p1", Text = "content" }],
            Suggestions = [suggestion]
        };

        doc.Suggestions.Should().HaveCount(1);
        doc.Suggestions[0].Id.Should().Be("sug-1");
    }

    [TestMethod]
    public void Serialization_ProducesCamelCasePropertyNames()
    {
        var doc = new Document
        {
            Id = "doc-1",
            Filename = "test.docx",
            Source = DocumentSource.Local,
            Paragraphs = [new Paragraph { Id = "p1", Text = "content" }]
        };

        var json = JsonSerializer.Serialize(doc);

        json.Should().Contain("\"id\":");
        json.Should().Contain("\"filename\":");
        json.Should().Contain("\"source\":");
        json.Should().Contain("\"paragraphs\":");
        json.Should().Contain("\"suggestions\":");
    }

    [TestMethod]
    public void Deserialization_FromCamelCaseJson_ReconstructsDocument()
    {
        const string json = """
        {
            "id": "doc-1",
            "filename": "test.docx",
            "source": "Local",
            "paragraphs": [{ "id": "p1", "text": "hello world" }],
            "suggestions": []
        }
        """;

        var doc = JsonSerializer.Deserialize<Document>(json);

        doc.Should().NotBeNull();
        doc!.Id.Should().Be("doc-1");
        doc.Filename.Should().Be("test.docx");
        doc.Source.Should().Be(DocumentSource.Local);
        doc.Paragraphs.Should().HaveCount(1);
        doc.Paragraphs[0].Text.Should().Be("hello world");
        doc.FullText.Should().Be("hello world");
        doc.Suggestions.Should().BeEmpty();
    }

    [TestMethod]
    public void Source_GoogleDocs_SerializesAsString()
    {
        var doc = new Document
        {
            Id = "doc-1",
            Filename = "imported.docx",
            Source = DocumentSource.GoogleDocs,
            Paragraphs = [new Paragraph { Id = "p1", Text = "content" }]
        };

        var json = JsonSerializer.Serialize(doc);
        json.Should().Contain("\"GoogleDocs\"");
    }

    [TestMethod]
    public void Record_Equality_MatchesOnAllProperties()
    {
        var paragraphs = new List<Paragraph> { new() { Id = "p1", Text = "content" } }.AsReadOnly();

        var doc1 = new Document
        {
            Id = "doc-1",
            Filename = "test.docx",
            Source = DocumentSource.Local,
            Paragraphs = paragraphs
        };

        var doc2 = new Document
        {
            Id = "doc-1",
            Filename = "test.docx",
            Source = DocumentSource.Local,
            Paragraphs = paragraphs
        };

        doc1.Should().Be(doc2);
    }

    [TestMethod]
    public void Record_With_CreatesModifiedCopy_WithoutMutatingOriginal()
    {
        var original = new Document
        {
            Id = "doc-1",
            Filename = "test.docx",
            Source = DocumentSource.Local,
            Paragraphs = [new Paragraph { Id = "p1", Text = "original content" }]
        };

        var modified = original with { Paragraphs = [new Paragraph { Id = "p1", Text = "revised content" }] };

        modified.FullText.Should().Be("revised content");
        modified.Id.Should().Be("doc-1");
        original.FullText.Should().Be("original content");
    }

    [TestMethod]
    public void Paragraphs_Empty_IsValid()
    {
        var doc = new Document
        {
            Id = "doc-1",
            Filename = "empty.docx",
            Source = DocumentSource.Local
        };

        doc.Paragraphs.Should().BeEmpty();
        doc.FullText.Should().BeEmpty();
    }

    [TestMethod]
    public void FullText_MultipleParagraphs_JoinsWithDoubleNewline()
    {
        var doc = new Document
        {
            Id = "doc-1",
            Filename = "long-manuscript.docx",
            Source = DocumentSource.Local,
            Paragraphs =
            [
                new Paragraph { Id = "p1", Text = "First paragraph." },
                new Paragraph { Id = "p2", Text = "Second paragraph." },
                new Paragraph { Id = "p3", Text = "Third paragraph." }
            ]
        };

        doc.FullText.Should().Be("First paragraph.\n\nSecond paragraph.\n\nThird paragraph.");
    }

    [TestMethod]
    public void GetParagraphIndex_ReturnsParagraphIndex()
    {
        var doc = new Document
        {
            Id = "doc-1",
            Filename = "test.docx",
            Source = DocumentSource.Local,
            Paragraphs =
            [
                new Paragraph { Id = "p1", Text = "First" },
                new Paragraph { Id = "p2", Text = "Second" }
            ]
        };

        doc.GetParagraphIndex("p1").Should().Be(0);
        doc.GetParagraphIndex("p2").Should().Be(1);

        var act = () => doc.GetParagraphIndex("nonexistent");
        act.Should().Throw<ArgumentException>();
    }
}
